namespace EnglishLearning.Application.Billing;

public sealed record VerifyGooglePurchaseRequest(string ProductId, string PurchaseToken);
