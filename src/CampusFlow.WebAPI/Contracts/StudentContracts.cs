using System.ComponentModel.DataAnnotations;

namespace CampusFlow.WebAPI.Contracts;

public record StudentResponse(int Id, string FirstName, string LastName, string Email);

public record CreateStudentRequest(
    [Required, StringLength(50)] string FirstName,
    [Required, StringLength(50)] string LastName,
    [Required, EmailAddress] string Email);
