// חריגה: הפרת כלל עסקי בקלט (שעת סיום לפני התחלה, מועד בעבר) -> 400.
namespace BabyLink.Application.Exceptions;

public class BusinessRuleException(string message) : Exception(message);
