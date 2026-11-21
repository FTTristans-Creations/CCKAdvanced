using System;
using JetBrains.Annotations;
using UnityEngine;

namespace ABI.CCK.Core
{
    [PublicAPI] // So rider doesn't complain about unused code
    public class UnityVersion : IComparable<UnityVersion>, IEquatable<UnityVersion>
    {
        public readonly int Major;
        public readonly int Minor;
        public readonly int Patch;
        public readonly string Build;

        // Maps different versioning schemes to a sequential order
        public int EffectiveMajorVersion
        {
            get
            {
                // ReSharper disable once PatternIsRedundant
                return Major switch
                {
                    // Map 2017 to 6, 2018 to 7, ..., 2023 to 12
                    >= 2017 and <= 2023 => Major - 2017 + 6,
                    // Unity 1 to 5
                    <= 5 => Major,
                    // Unity 6 and above (from 2024 onwards)
                    >= 6 => Major + 7 // Unity 6 becomes 13, Unity 7 becomes 14, etc.
                    // Lets just hope Unity doesn't make it to version 2017 in the next 100 years lol
                };
            }
        }

        public UnityVersion(string versionString)
        {
            try
            {
                string[] parts = versionString.Split('.');

                if (parts.Length >= 1)
                    Major = int.Parse(parts[0]);

                if (parts.Length >= 2)
                    Minor = int.Parse(parts[1]);

                if (parts.Length < 3) return;
                string patchPart = parts[2];

                // Handle build suffixes like 'f1', 'a', 'b', 'p1', etc.
                int index = 0;
                while (index < patchPart.Length && char.IsDigit(patchPart[index]))
                    index++;

                if (index > 0)
                    Patch = int.Parse(patchPart[..index]);

                Build = patchPart[index..];
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse Unity version string '{versionString}': {e.Message}");
                Major = 0;
                Minor = 0;
                Patch = 0;
                Build = string.Empty;
            }
        }

        public static UnityVersion GetCurrent() => new(Application.unityVersion);

        public int CompareTo(UnityVersion other)
        {
            if (other == null) return 1;

            int majorComparison = EffectiveMajorVersion.CompareTo(other.EffectiveMajorVersion);
            if (majorComparison != 0) return majorComparison;

            int minorComparison = Minor.CompareTo(other.Minor);
            if (minorComparison != 0) return minorComparison;

            int patchComparison = Patch.CompareTo(other.Patch);
            if (patchComparison != 0) return patchComparison;

            return string.Compare(Build, other.Build, StringComparison.Ordinal);
        }

        public bool Equals(UnityVersion other)
        {
            if (other == null) return false;
            return EffectiveMajorVersion == other.EffectiveMajorVersion &&
                   Minor == other.Minor &&
                   Patch == other.Patch &&
                   Build == other.Build;
        }

        public override bool Equals(object obj) => obj is UnityVersion other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(EffectiveMajorVersion, Minor, Patch, Build);

        public override string ToString() => $"{Major}.{Minor}.{Patch}{Build}";

        public static bool operator >(UnityVersion v1, UnityVersion v2) => v1.CompareTo(v2) > 0;

        public static bool operator <(UnityVersion v1, UnityVersion v2) => v1.CompareTo(v2) < 0;

        public static bool operator >=(UnityVersion v1, UnityVersion v2) => v1.CompareTo(v2) >= 0;

        public static bool operator <=(UnityVersion v1, UnityVersion v2) => v1.CompareTo(v2) <= 0;

        public static bool operator ==(UnityVersion v1, UnityVersion v2)
        {
            if (ReferenceEquals(v1, null) && ReferenceEquals(v2, null)) return true;
            if (ReferenceEquals(v1, null) || ReferenceEquals(v2, null)) return false;

            return v1.Equals(v2);
        }

        public static bool operator !=(UnityVersion v1, UnityVersion v2) => !(v1 == v2);
    }
    
    // [TestFixture]
    // public class UnityVersionTests
    // {
    //     [Test]
    //     public void TestParseVersionString()
    //     {
    //         var versionStrings = new[]
    //         {
    //             "5.6.7f1",
    //             "2017.4.40f1",
    //             "2019.3.15f1",
    //             "2020.1.0b5",
    //             "2021.2.0a1",
    //             "2022.1.10p2",
    //             "2023.1.0f1",
    //             "6.0.0f1",  // Unity 2024
    //             "7.0.1f2",  // Unity 2025
    //         };
    //
    //         foreach (var versionString in versionStrings)
    //         {
    //             UnityVersion unityVersion = new(versionString);
    //             Console.WriteLine($"Parsed {versionString} => {unityVersion}");
    //         }
    //     }
    //
    //     [Test]
    //     public void TestEffectiveMajorVersion()
    //     {
    //         UnityVersion unity5 = new("5.6.7f1");
    //         UnityVersion unity2017 = new("2017.4.40f1");
    //         UnityVersion unity2023 = new("2023.1.0f1");
    //         UnityVersion unity6 = new("6.0.0f1");  // Unity 2024
    //         UnityVersion unity7 = new("7.0.0f1");  // Unity 2025
    //
    //         Assert.AreEqual(5, unity5.EffectiveMajorVersion);
    //         Assert.AreEqual(6, unity2017.EffectiveMajorVersion);
    //         Assert.AreEqual(12, unity2023.EffectiveMajorVersion);
    //         Assert.AreEqual(13, unity6.EffectiveMajorVersion);
    //         Assert.AreEqual(14, unity7.EffectiveMajorVersion);
    //     }
    //
    //     [Test]
    //     public void TestVersionComparison()
    //     {
    //         UnityVersion unity5 = new("5.6.7f1");
    //         UnityVersion unity2017 = new("2017.4.40f1");
    //         UnityVersion unity2018 = new("2018.4.36f1");
    //         UnityVersion unity2020 = new("2020.3.18f1");
    //         UnityVersion unity2021 = new("2021.1.15f1");
    //         UnityVersion unity2022 = new("2022.2.0a13");
    //         UnityVersion unity2023 = new("2023.1.0b1");
    //         UnityVersion unity6 = new("6.0.0f1");  // Unity 2024
    //         UnityVersion unity7 = new("7.0.0f1");  // Unity 2025
    //
    //         // Unity 5 < Unity 2017
    //         Assert.IsTrue(unity5 < unity2017);
    //
    //         // Unity 2017 < Unity 2018
    //         Assert.IsTrue(unity2017 < unity2018);
    //
    //         // Unity 2022 < Unity 2023
    //         Assert.IsTrue(unity2022 < unity2023);
    //
    //         // Unity 2023 < Unity 6 (2024)
    //         Assert.IsTrue(unity2023 < unity6);
    //
    //         // Unity 6 (2024) < Unity 7 (2025)
    //         Assert.IsTrue(unity6 < unity7);
    //
    //         // Unity 2021 == 2021.1.15f1
    //         UnityVersion anotherUnity2021 = new("2021.1.15f1");
    //         Assert.IsTrue(unity2021 == anotherUnity2021);
    //
    //         // Unity 2020.3.18f1 > Unity 2020.1.0b5
    //         UnityVersion unity2020Beta = new("2020.1.0b5");
    //         Assert.IsTrue(unity2020 > unity2020Beta);
    //
    //         // Check ordering in a list
    //         var versions = new[] { unity7, unity5, unity2018, unity6, unity2023, unity2017 };
    //         Array.Sort(versions);
    //         var expectedOrder = new[] { unity5, unity2017, unity2018, unity2023, unity6, unity7 };
    //
    //         CollectionAssert.AreEqual(expectedOrder, versions);
    //     }
    //
    //     [Test]
    //     public void TestEquality()
    //     {
    //         UnityVersion versionA = new("2021.1.15f1");
    //         UnityVersion versionB = new("2021.1.15f1");
    //         UnityVersion versionC = new("2021.1.16f1");
    //
    //         Assert.IsTrue(versionA.Equals(versionB));
    //         Assert.IsFalse(versionA.Equals(versionC));
    //
    //         Assert.IsTrue(versionA == versionB);
    //         Assert.IsTrue(versionA != versionC);
    //     }
    //
    //     [Test]
    //     public void TestHashCodeConsistency()
    //     {
    //         UnityVersion versionA = new("2022.1.10p2");
    //         UnityVersion versionB = new("2022.1.10p2");
    //
    //         Assert.AreEqual(versionA.GetHashCode(), versionB.GetHashCode());
    //     }
    //
    //     [Test]
    //     public void TestToString()
    //     {
    //         UnityVersion version = new("2019.3.15f1");
    //         Assert.AreEqual("2019.3.15f1", version.ToString());
    //     }
    //
    //     [Test]
    //     public void TestInvalidVersionString()
    //     {
    //         Assert.Throws<FormatException>(() => _ = new UnityVersion("Invalid.Version.String"));
    //         Assert.Throws<FormatException>(() => _ = new UnityVersion("2020.A.Bf1"));
    //     }
    //
    //     [Test]
    //     public void TestNullComparison()
    //     {
    //         UnityVersion version = new("2021.1.15f1");
    //         UnityVersion nullVersion = null;
    //
    //         // ReSharper disable twice ExpressionIsAlwaysNull
    //         Assert.IsTrue(version.CompareTo(nullVersion) > 0);
    //         Assert.IsFalse(version.Equals(nullVersion));
    //         Assert.IsFalse(version == nullVersion);
    //         Assert.IsTrue(version != nullVersion);
    //     }
    // }
}