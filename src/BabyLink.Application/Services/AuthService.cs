// מימוש IAuthService.
// תלויות (Constructor): IUserRepository, IPasswordHasher, IJwtTokenGenerator, IMapper, ILogger.
// הרשמה: אימייל קיים -> ConflictException (409). התחברות שגויה -> InvalidCredentialsException (401).
// לוגים: Information על הרשמה/התחברות - בלי סיסמה, בלי token!
