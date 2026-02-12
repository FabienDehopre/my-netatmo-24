using MyNetatmo24.SharedKernel.StronglyTypedIds;

namespace MyNetatmo24.SharedKernel.Messages;

public sealed record AccountCreated(AccountId Id, string FirstName, string LastName);
