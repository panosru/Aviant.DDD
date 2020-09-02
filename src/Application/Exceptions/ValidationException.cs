namespace Aviant.DDD.Application.Exceptions
{
    using System.Collections.Generic;
    using System.Linq;
    using FluentValidation.Results;

    public class ValidationException : ApplicationException
    {
        public ValidationException()
            : base("One or more validation failures have occurred.")
        {
            Failures = new Dictionary<string, string[]>();
        }

        public ValidationException(IEnumerable<ValidationFailure> failures)
            : this()
        {
            IEnumerable<IGrouping<string, string>> failureGroups = failures
               .GroupBy(e => e.PropertyName, e => e.ErrorMessage);

            foreach (IGrouping<string, string> failureGroup in failureGroups)
            {
                var      propertyName     = failureGroup.Key;
                string[] propertyFailures = failureGroup.ToArray();

                Failures.Add(propertyName, propertyFailures);
            }
        }

        public IDictionary<string, string[]> Failures { get; }
    }
}