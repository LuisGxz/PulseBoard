namespace PulseBoard.Application.Common.Exceptions;

/// <summary>Maps to 404.</summary>
public class NotFoundException(string entity, object key) : Exception($"{entity} '{key}' was not found.");

/// <summary>Maps to 401. Message is safe to expose to clients.</summary>
public class UnauthorizedException(string message) : Exception(message);

/// <summary>Maps to 403. The principal is authenticated but not allowed to perform this action.</summary>
public class ForbiddenException(string message) : Exception(message);

/// <summary>Maps to 409.</summary>
public class ConflictException(string message) : Exception(message);

/// <summary>Maps to 502. The ETL microservice rejected the upload or was unreachable.</summary>
public class EtlException(string message) : Exception(message);
