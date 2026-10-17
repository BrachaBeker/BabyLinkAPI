// חריגה: התנגשות Concurrency - מישהי אחרת שמרה בין הקריאה לשמירה -> 409 + לוג Warning.
// נזרקת מה-Repository במקום DbUpdateConcurrencyException, כדי ש-Application לא תכיר את EF.
