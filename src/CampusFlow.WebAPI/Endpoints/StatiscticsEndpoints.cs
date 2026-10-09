using CampusFlow.WebAPI.Contracts;
using CampusFlow.WebAPI.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace CampusFlow.WebAPI.Endpoints;

public static class StatisticsEndpoints
{
    public static RouteHandlerBuilder MapStatisticsEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGet("/api/statistics", GetStatistics).WithTags("Statistics");

    static async Task<Ok<StatisticsResponse>> GetStatistics(CampusFlowDbContext db)
    {
        // Aggregates: each call sends one SQL statement.
        var courseCount = await db.Courses.CountAsync();
        var studentCount = await db.Students.CountAsync();
        var enrollmentCount = await db.Enrollments.CountAsync();

        // IQueryable: the database counts the enrollments.
        var courseData = await db.Courses
            .Select(c => new { c.Code, c.Capacity, Enrolled = c.Enrollments.Count })
            .ToListAsync();

        // IEnumerable: the calculation and the sort run in memory.
        var occupancy = courseData
            .Select(c => new CourseOccupancy(
                c.Code, c.Capacity, c.Enrolled, Math.Round(100.0 * c.Enrolled / c.Capacity, 1)))
            .OrderByDescending(c => c.OccupancyPercent)
            .ToList();

        // GroupBy: the database groups the courses by credits.
        var creditGroups = await db.Courses
            .GroupBy(c => c.Credits)
            .Select(g => new CreditGroup(g.Key, g.Count(), g.Sum(c => c.Capacity)))
            .ToListAsync();

        // Query syntax: the compiler changes it into method syntax.
        var studentsWithoutCourse = await (
            from s in db.Students
            where !s.Enrollments.Any()
            orderby s.LastName
            select s.FirstName + " " + s.LastName).ToListAsync();

        return TypedResults.Ok(new StatisticsResponse(
            courseCount, studentCount, enrollmentCount, occupancy, creditGroups, studentsWithoutCourse));
    }
}
