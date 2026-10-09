using CampusFlow.WebAPI.Contracts;
using CampusFlow.WebAPI.Data;
using CampusFlow.WebAPI.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace CampusFlow.WebAPI.Endpoints;

public static class EnrollmentEndpoints
{
    public static RouteGroupBuilder MapEnrollmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/courses/{courseId:int}/enrollments").WithTags("Enrollments");

        group.MapGet("/", GetEnrollments);
        group.MapPost("/", EnrollStudent);
        group.MapDelete("/{studentId:int}", RemoveEnrollment);

        return group;
    }

    static async Task<Results<Ok<List<EnrollmentResponse>>, NotFound>> GetEnrollments(
        int courseId, CampusFlowDbContext db)
    {
        if (!await db.Courses.AnyAsync(c => c.Id == courseId))
        {
            return TypedResults.NotFound();
        }

        var enrollments = await db.Enrollments
            .Where(e => e.CourseId == courseId)
            .OrderBy(e => e.Student.LastName)
            .Select(e => new EnrollmentResponse(
                e.CourseId, e.StudentId, e.Student.FirstName + " " + e.Student.LastName, e.EnrolledAt))
            .ToListAsync();

        return TypedResults.Ok(enrollments);
    }

    static async Task<Results<Created<EnrollmentResponse>, ProblemHttpResult>> EnrollStudent(
        int courseId, EnrollStudentRequest request, CampusFlowDbContext db)
    {
        var course = await db.Courses
            .Where(c => c.Id == courseId)
            .Select(c => new { c.Capacity, EnrolledCount = c.Enrollments.Count })
            .FirstOrDefaultAsync();

        if (course is null)
        {
            return Problem(StatusCodes.Status404NotFound, "Course not found.", $"Course {courseId} does not exist.");
        }

        var student = await db.Students.FindAsync(request.StudentId);
        if (student is null)
        {
            return Problem(StatusCodes.Status404NotFound, "Student not found.", $"Student {request.StudentId} does not exist.");
        }

        if (await db.Enrollments.AnyAsync(e => e.CourseId == courseId && e.StudentId == student.Id))
        {
            return Problem(StatusCodes.Status409Conflict, "Already enrolled.", "The student is already enrolled in this course.");
        }

        if (course.EnrolledCount >= course.Capacity)
        {
            return Problem(StatusCodes.Status409Conflict, "Course full.", "The course has no free places.");
        }

        var enrollment = new Enrollment
        {
            CourseId = courseId,
            StudentId = student.Id,
            EnrolledAt = DateTime.UtcNow
        };

        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        var response = new EnrollmentResponse(
            courseId, student.Id, $"{student.FirstName} {student.LastName}", enrollment.EnrolledAt);
        return TypedResults.Created($"/api/courses/{courseId}/enrollments", response);
    }

    static async Task<Results<NoContent, NotFound>> RemoveEnrollment(
        int courseId, int studentId, CampusFlowDbContext db)
    {
        var deleted = await db.Enrollments
            .Where(e => e.CourseId == courseId && e.StudentId == studentId)
            .ExecuteDeleteAsync();

        return deleted == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }

    static ProblemHttpResult Problem(int statusCode, string title, string detail) =>
        TypedResults.Problem(statusCode: statusCode, title: title, detail: detail);
}
