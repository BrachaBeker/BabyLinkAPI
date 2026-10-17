// Unit Tests (xUnit + Moq) ל-ServiceRequestService - Repositories מדומים.
// חובה: AssignAsync_WhenAvailable_AssignsBabysitter (מקרה תקין).
// חובה: AssignAsync_WhenRepositoryThrowsConcurrencyConflict_PropagatesConflict (מקרה נדחה).
// נוספים:
//   AssignAsync_WhenAssignedToOtherBabysitter_ThrowsConflict
//   AssignAsync_WhenAlreadyAssignedToSameBabysitter_Succeeds (אידמפוטנטיות)
//   AssignAsync_WhenRequestNotFound_ThrowsNotFound
//   AssignAsync_WhenStartTimePassed_ThrowsBusinessRule
//   AssignAsync_WhenBabysitterHasOverlappingAssignment_ThrowsConflict
//   UpdateAsync_WhenNotOwner_ThrowsForbidden
//   UpdateAsync_WhenAlreadyAssigned_ThrowsConflict
