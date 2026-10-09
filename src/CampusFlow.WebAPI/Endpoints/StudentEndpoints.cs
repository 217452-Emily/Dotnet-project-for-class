using CampusFlow.WebAPI.Contracts;
using CampusFlow.WebAPI.Data;
using CampusFlow.WebAPI.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace CampusFlow.WebAPI.Endpoints;

public static class StudentEndpoints
{
    public static RouteGroupBuilder MapStudentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/students").WithTags("Students");

        group.MapGet("/", GetStudents);
        group.MapGet("/{id:int}", GetStudent);
        group.MapPost("/", CreateStudent);

        return group;
    }

    static async Task<Ok<List<StudentResponse>>> GetStudents(CampusFlowDbContext db)
    {
        var students = await db.Students
            .OrderBy(s => s.LastName)
            .Select(s => new StudentResponse(s.Id, s.FirstName, s.LastName, s.Email))
            .ToListAsync();

        return TypedResults.Ok(students);
    }

    static async Task<Results<Ok<StudentResponse>, NotFound>> GetStudent(int id, CampusFlowDbContext db)
    {
        var student = await db.Students.FindAsync(id);

        return student is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new StudentResponse(student.Id, student.FirstName, student.LastName, student.Email));
    }

    static async Task<Results<Created<StudentResponse>, ProblemHttpResult>> CreateStudent(
        CreateStudentRequest request, CampusFlowDbContext db)
    {
        if (await db.Students.AnyAsync(s => s.Email == request.Email))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Email address already exists.",
                detail: $"A student with the email address '{request.Email}' already exists.");
        }

        var student = new Student
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email
        };

        db.Students.Add(student);
        await db.SaveChangesAsync();

        var response = new StudentResponse(student.Id, student.FirstName, student.LastName, student.Email);
        return TypedResults.Created($"/api/students/{student.Id}", response);
    }
}
