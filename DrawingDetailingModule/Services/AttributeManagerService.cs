using NXOpen;
using DrawingDetailingModule.Constants;

namespace DrawingDetailingModule.Services
{
    /// <summary>
    /// Reads title-block user attributes off a specific part. Takes the target <see cref="Part"/>
    /// explicitly rather than reading Session's current work part, so callers can pass the exact
    /// part the attributes should be read from regardless of what is current at read time.
    /// </summary>
    public static class AttributeManagerService
    {
        public static string GetAttribute(Part part, string category, string title)
        {
            AttributeIterator itr = part.CreateAttributeIterator();
            itr.SetIncludeOnlyCategory(category);
            itr.SetIncludeOnlyTitle(title);

            foreach (var att in part.GetUserAttributes(itr))
            {
                if (att.Title == title)
                    return att.StringValue;
            }

            return null;
        }

        public static string GetDesignBy(Part part) =>
            GetAttribute(part, AttributeNames.CATEGORY_TITLEBLOCK, AttributeNames.TITLE_DESIGNBY);

        public static string GetModelName(Part part) =>
            GetAttribute(part, AttributeNames.CATEGORY_TITLEBLOCK, AttributeNames.TITLE_MODEL_NAME);

        public static string GetPartName(Part part) =>
            GetAttribute(part, AttributeNames.CATEGORY_TITLEBLOCK, AttributeNames.TITLE_PART);
    }
}
