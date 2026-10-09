using CampusFlow.WebAPI.Contracts;
using CampusFlow.WebAPI.Data;
using CampusFlow.WebAPI.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace CampusFlow.WebAPI.Endpoints;

public static class CourseEndpoints
{
    public static RouteGroupBuilder MapCourseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/courses").WithTags("Courses");

        group.MapGet("/", GetCourses);
        group.MapGet("/{id:int}", GetCourse);
        group.MapPost("/", CreateCourse);
        group.MapPut("/{id:int}", UpdateCourse);
        group.MapDelete("/{id:int}", DeleteCourse);

        return group;
    }

    static async Task<Ok<List<CourseResponse>>> GetCourses(
    CampusFlowDbContext db, string? search, int? minCredits)
    {
    var query = db.Courses.AsQueryable();

    if (!string.IsNullOrWhiteSpace(search))
    {
        query = query.Where(c => c.Code.Contains(search) || c.Title.Contains(search));
    }

    if (minCredits is not null)
    {
        query = query.Where(c => c.Credits >= minCredits);
    }

    var courses = await query
        .OrderBy(c => c.Code)
        .Select(c => new CourseResponse(c.Id, c.Code, c.Title, c.Credits, c.Capacity, c.Enrollments.Count))
        .ToListAsync();

    return TypedResults.Ok(courses);
    }


    static async Task<Results<Ok<CourseResponse>, NotFound>> GetCourse(int id, CampusFlowDbContext db)
    {
        var course = await db.Courses
            .Where(c => c.Id == id)
            .Select(c => new CourseResponse(c.Id, c.Code, c.Title, c.Credits, c.Capacity, c.Enrollments.Count))
            .FirstOrDefaultAsync();

        return course is null ? TypedResults.NotFound() : TypedResults.Ok(course);
    }

    static async Task<Results<Created<CourseResponse>, ProblemHttpResult>> CreateCourse(
        CreateCourseRequest request, CampusFlowDbContext db)
    {
        if (await db.Courses.AnyAsync(c => c.Code == request.Code))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Course code already exists.",
                detail: $"A course with the code '{request.Code}' already exists.");
        }

        var course = new Course
        {
            Code = request.Code,
            Title = request.Title,
            Credits = request.Credits,
            Capacity = request.Capacity
        };

        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var response = new CourseResponse(course.Id, course.Code, course.Title, course.Credits, course.Capacity, 0);
        return TypedResults.Created($"/api/courses/{course.Id}", response);
    }

    static async Task<Results<NoContent, NotFound>> UpdateCourse(
        int id, UpdateCourseRequest request, CampusFlowDbContext db)
    {
        var course = await db.Courses.FindAsync(id);
        if (course is null)
        {
            return TypedResults.NotFound();
        }

        course.Title = request.Title;
        course.Credits = request.Credits;
        course.Capacity = request.Capacity;
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    static async Task<Results<NoContent, NotFound>> DeleteCourse(int id, CampusFlowDbContext db)
    {
        var deleted = await db.Courses.Where(c => c.Id == id).ExecuteDeleteAsync();
        return deleted == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }
}
