// חריגה: המשאב לא נמצא -> 404.
namespace BabyLink.Application.Exceptions;

public class NotFoundException(string message) : Exception(message);
