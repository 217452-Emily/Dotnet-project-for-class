using System.ComponentModel.DataAnnotations;

namespace CampusFlow.WebAPI.Contracts;

public record EnrollmentResponse(int CourseId, int StudentId, string StudentName, DateTime EnrolledAt);

public record EnrollStudentRequest([Range(1, int.MaxValue)] int StudentId);
