module LibraryAnalysis.Analysis

open Shared.TagLibrary
open Shared.Utilities

let albumArtPercentage (tags: LibraryTags array) =
    tags
    |> Array.choose (fun t -> if t.ImageCount > 0 then Some t.ImageCount else None)
    |> Array.length
    |> fun count -> float count / float tags.Length * 100.
    |> fun x -> $"With album art: %s{formatFloat x}%%"

