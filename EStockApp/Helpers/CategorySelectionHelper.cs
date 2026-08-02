using System.Collections.Generic;
using TinyPinyin;

namespace EStockApp.Helpers;

public static class CategorySelectionHelper
{
    public static bool MatchesLetter(string? category, char letter)
    {
        if (string.IsNullOrEmpty(category))
            return false;

        letter = char.ToUpperInvariant(letter);

        if (char.ToUpperInvariant(category[0]) == letter)
            return true;

        var initials = PinyinHelper.GetPinyinInitials(category);
        return !string.IsNullOrEmpty(initials) && char.ToUpperInvariant(initials[0]) == letter;
    }

    public static bool TrySelectNext(IList<string> categories, string? current, char letter, out string? selected)
    {
        selected = null;
        if (categories.Count == 0)
            return false;

        letter = char.ToUpperInvariant(letter);
        var currentIndex = string.IsNullOrEmpty(current) ? -1 : categories.IndexOf(current!);
        var start = currentIndex < 0 ? 0 : currentIndex + 1;

        for (var offset = 0; offset < categories.Count; offset++)
        {
            var index = (start + offset) % categories.Count;
            var category = categories[index];
            if (!MatchesLetter(category, letter))
                continue;

            selected = category;
            return true;
        }

        return false;
    }
}
