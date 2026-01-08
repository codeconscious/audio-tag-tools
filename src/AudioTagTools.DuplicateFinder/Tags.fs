module DuplicateFinder.Tags

open Errors
open Settings
open Shared
open Shared.TagLibrary
open System
open System.IO
open System.Text

let parseToTags json =
    json
    |> parseJsonToTags
    |> Result.mapError TagParseError

let filter (settings: Settings) (allTags: MultipleLibraryTags) : MultipleLibraryTags =
    let(|ArtistAndTitle|ArtistOnly|TitleOnly|Invalid|) (exclusion: Exclusion) =
        match exclusion.Artist, exclusion.Title with
        | Some a, Some t -> ArtistAndTitle (a, t)
        | Some a, None -> ArtistOnly a
        | None, Some t -> TitleOnly t
        | _ -> Invalid

    let isIncluded (fileTags: LibraryTags) =
        let isExcluded fileTags = function
            | ArtistAndTitle (artist, title) ->
                Array.contains artist fileTags.AlbumArtists ||
                Array.contains artist fileTags.Artists ||
                fileTags.Title.StartsWith(title, StringComparison.InvariantCultureIgnoreCase)
            | ArtistOnly artist ->
                Array.contains artist fileTags.AlbumArtists ||
                Array.contains artist fileTags.Artists
            | TitleOnly title ->
                fileTags.Title.StartsWith(title, StringComparison.InvariantCultureIgnoreCase)
            | Invalid -> false

        settings.Exclusions
        |> Array.exists (isExcluded fileTags)
        |> not

    allTags
    |> Array.filter isIncluded

/// Returns a normalized string of two concatenated items:
/// (1) The artist name that should be used for artist grouping when searching for duplicates.
///     If the artist appears in any "equivalent artist" group, then the first equivalent artist
///     name from that group will be prioritized over the track's artist name.
/// (2) The track title.
/// The string is intended to be used solely for track grouping.
let private groupName (settings: Settings) fileTags =
    let scrubText subStrs =
        [| String.whiteSpaces; subStrs |]
        |> Array.concat
        |> String.stripSubstrings
        >> String.stripPunctuation

    let artist =
        let checkEquivalentArtists trackArtist =
            settings.EquivalentArtists
            |> Array.tryFind (Array.contains trackArtist)
            |> function
               | Some eqArtists -> eqArtists[0]
               | None -> trackArtist

        fileTags
        |> mainArtists String.Empty
        |> checkEquivalentArtists
        |> scrubText settings.ArtistReplacements

    let title =
        fileTags.Title
        |> scrubText settings.TitleReplacements

    $"{artist}{title}"
        .Normalize(NormalizationForm.FormC)
        .ToLowerInvariant()

let findDuplicates settings (tags: MultipleLibraryTags) : MultipleLibraryTags array option =
    tags
    |> Array.filter hasArtistAndTitle
    |> Array.groupBy (groupName settings)
    |> Array.filter (fun (_, tags) -> Array.hasMultiple tags)
    |> Array.sortBy fst // Group name
    |> Array.map (fun (_, tags) -> tags |> Array.sortBy (mainArtists String.Empty))
    |> Array.toOption

let printCount description (tags: MultipleLibraryTags) =
    printfn $"%s{description}%s{String.formatInt tags.Length}"

let printDuplicates (groupedTracks: MultipleLibraryTags array option) =
    let printfGray = printfColor ConsoleColor.DarkGray

    let printGroup index (groupTracks: MultipleLibraryTags) =
        let artistSummary (track: LibraryTags) : string =
            if Array.isEmpty track.Artists
            then String.Empty
            else String.Join(", ", track.Artists)

        let printFileSummary fileTags =
            let artist = artistSummary fileTags
            let title = fileTags.Title
            let duration = String.formatTimeSpan fileTags.Duration
            let extNoPeriod = (Path.GetExtension fileTags.FileName)[1..] |> _.ToUpperInvariant()
            let bitRate = $"{fileTags.BitRate}kbps"
            let fileSize = String.formatBytes fileTags.FileSize
            printf $"    • {artist}"
            printfGray " — "
            printf $"{title}"
            printfGray $"  [{duration} {extNoPeriod} {bitRate} {fileSize}]{Environment.NewLine}"

        let printHeader () =
            groupTracks
            |> Array.head
            |> mainArtists ", "
            |> printfn "%d. %s" (index + 1) // Start numbering at 1, not 0.

        let printDuplicates () =
            groupTracks
            |> Array.iter printFileSummary

        printHeader ()
        printDuplicates ()

    match groupedTracks with
    | None -> printfn "No duplicates found."
    | Some gt -> gt |> Array.iteri printGroup
