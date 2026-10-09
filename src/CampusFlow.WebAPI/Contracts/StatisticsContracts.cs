namespace CampusFlow.WebAPI.Contracts;

public record CourseOccupancy(string Code, int Capacity, int Enrolled, double OccupancyPercent);

public record CreditGroup(int Credits, int CourseCount, int TotalCapacity);

public record StatisticsResponse(
    int CourseCount,
    int StudentCount,
    int EnrollmentCount,
    List<CourseOccupancy> Occupancy,
    List<CreditGroup> CreditGroups,
    List<string> StudentsWithoutCourse);
