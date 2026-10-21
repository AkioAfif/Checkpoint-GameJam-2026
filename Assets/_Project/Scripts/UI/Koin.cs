using System.Globalization;

// Money as the village writes it: dots between thousands ("48.500.000").
public static class Koin
{
    // invariant + swap instead of the id-ID culture, which WebGL builds may not ship
    public static string Format(long amount) => amount.ToString("N0", CultureInfo.InvariantCulture).Replace(',', '.');
}
