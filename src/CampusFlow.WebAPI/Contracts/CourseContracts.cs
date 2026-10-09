namespace CampusFlow.WebAPI.Contracts;

public record CourseResponse(int Id, string Code, string Title, int Credits, int Capacity, int EnrolledCount);

public record CreateCourseRequest(string Code, string Title, int Credits, int Capacity);

public record UpdateCourseRequest(string Title, int Credits, int Capacity);
