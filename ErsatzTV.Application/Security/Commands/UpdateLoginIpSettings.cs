namespace ErsatzTV.Application.Security;

public record UpdateLoginIpSettings(LoginIpSettingsViewModel Settings) : IRequest<Either<BaseError, Unit>>;
