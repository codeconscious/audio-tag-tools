module DuplicateFinder.Tags

open Errors
open Settings
open Shared
open Shared.TagLibrary
open CCFSharpUtils.Library
open System
open System.IO
open System.Text

let parseToTags json =
    json
    |> parseToTags
    |> Result.mapError TagParseError

/// Filters out tags containing artists or titles specified in the exclusions in the settings.
let filter (settings: Settings) allTags : MultipleLibraryTags =
    let isExcluded tags =
        let (|ArtistAndTitle|ArtistOnly|TitleOnly|Invalid|) (excl: Exclusion) =
            match excl.Artist, excl.Title with
            | Some a, Some t -> ArtistAndTitle (a, t)
            | Some a, None   -> ArtistOnly a
            | None,   Some t -> TitleOnly t
            | _ -> Invalid

        let containsArtist a = [| tags.AlbumArtists; tags.Artists |] |> Array.anyContainsIgnoreCase a
        let titleStartsWith t = tags.Title |> String.startsWithIgnoreCase t

        let check = function
            | ArtistAndTitle (a, t) -> containsArtist a && titleStartsWith t
            | ArtistOnly a -> containsArtist a
            | TitleOnly t -> titleStartsWith t
            | Invalid -> false

        settings.Exclusions
        |> Array.exists check

    allTags
    |> Array.filter (not << isExcluded)

/// Returns a normalized string of two concatenated items:
/// (1) The artist name that should be used for artist grouping when searching for duplicates.
///     If the artist appears in any "equivalent artist" group, then the first equivalent artist
///     name from that group will be prioritized over the track's artist name.
/// (2) The track title.
/// The string is intended to be used solely for track grouping.
let private groupName (settings: Settings) fileTags =
    let scrubText subStrs =
        subStrs
        |> Array.append (String.whiteSpaces |> Array.map _.ToString())
        |> String.stripSubstrings
        >> String.stripPunctuation

    let artist =
        let checkEquivalentArtists artist =
            settings.EquivalentArtists
            |> Array.tryFind (Array.contains artist)
            |> Option.map Array.head
            |> Option.defaultValue artist

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
        let artistSummary (tags: LibraryTags) : string =
            if Array.isEmpty tags.Artists
            then String.Empty
            else String.Join(", ", tags.Artists)

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
    | Some group -> group |> Array.iteri printGroup
