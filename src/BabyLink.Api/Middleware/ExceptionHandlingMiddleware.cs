// Middleware ראשון ב-Pipeline - תופס כל חריגה וממפה לתשובת JSON אחידה (ProblemDetails):
// NotFound->404, Forbidden->403, BusinessRule->400, InvalidCredentials->401,
// Conflict/ConcurrencyConflict->409 (לוג Warning), כל השאר->500 (לוג Error, בלי לחשוף פרטים).
// מוסיף לתשובה את ה-CorrelationId (נקרא מ-HttpContext.Items).
