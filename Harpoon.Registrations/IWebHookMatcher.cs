namespace Harpoon.Registrations
{
    /// <summary>
    /// Represents a class able to 
    /// </summary>
    public interface IWebHookMatcher
    {
        /// <summary>
        /// Returns a value indicating if the given <see cref="IWebHook"/> is suited for the given <see cref="IWebHookNotification"/>.
        /// This method should not be called in a LINQ-To-SQL query
        /// </summary>
        /// <param name="webHook"></param>
        /// <param name="notification"></param>
        /// <returns></returns>
        bool Matches(IWebHook webHook, IWebHookNotification notification);
    }
}