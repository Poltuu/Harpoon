namespace Harpoon.Registrations
{
    /// <summary>
    /// This <see langword="enum" /> exposes the different possible outcomes during a write operation on a <see cref="IWebHookRegistrationStore"/>
    /// </summary>
    public enum WebHookRegistrationStoreResult
    {
        /// <summary>
        /// This value means that everything went well
        /// </summary>
        Success,
        /// <summary>
        /// This value means that a requested object was not present
        /// </summary>
        NotFound,
        /// <summary>
        /// This value means that an unexpected error was caught
        /// </summary>
        InternalError
    }
}