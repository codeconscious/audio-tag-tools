module DuplicateFinder.Tags

open Errors
open Settings
open Shared
open Shared.TagLibrary
open FSharpPlus
open FSharpPlus.Data
open CCFSharpUtils
open System
open System.IO

let parseToTags json =
    json |> parseJsonToNonEmptyTags |! TagParseError

let printCount description (tags: LibraryTags NonEmptyList) =
    printfn $"%s{description}%s{String.formatInt tags.Length}"

/// Filters out tags containing artists or titles specified in the exclusions in the settings.
let discardExcluded
    (settings: Settings)
    (allTags: LibraryTags NonEmptyList)
    : Result<LibraryTags NonEmptyList, DupeFinderError> =

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
    |> NonEmptyList.tryFilter (not << isExcluded)
    |> Option.toResultWith NoFilesRemainAfterFiltering

/// Returns a normalized string of two concatenated items:
/// (1) The artist name that should be used for artist grouping when searching for duplicates.
///     If the artist appears in any "equivalent artist" group, then the first equivalent artist
///     name from that group will be prioritized over the track's artist name.
/// (2) The track title.
/// The string is intended to be used solely for track grouping.
let private groupName (settings: Settings) fileTags =
    let scrubText subStrs =
        subStrs
        |> Array.append (String.whiteSpaceStrs |> Array.ofList)
        |> String.stripSubstrings
        >> String.stripPunctuation
        >> String.stripDiacritics

    let artist =
        let checkEquivalentArtists artist =
            settings.EquivalentArtists
            |> Array.tryFind (Array.contains artist)
            |> option Array.head artist

        fileTags
        |> mainArtists String.Empty
        |> checkEquivalentArtists
        |> scrubText settings.ArtistReplacements

    let title =
        fileTags.Title
        |> scrubText settings.TitleReplacements

    $"{artist}{title}".ToLowerInvariant()

let findDuplicates settings (tags: LibraryTags NonEmptyList)
    : LibraryTags NonEmptyList NonEmptyList option =

    tags
    |> toList
    |> filter hasArtistAndTitle
    |> groupBy (groupName settings)
    |> filter (snd >> List.hasMultiple)
    |> sortBy fst // Group name
    |> map (snd >> sortBy (mainArtists String.Empty) >> NonEmptyList.ofList)
    |> List.toNonEmptyListOption

let printDuplicates (groupedTracks: LibraryTags NonEmptyList NonEmptyList option) : unit =
    let printfGray = printfColor ConsoleColor.DarkGray

    let printGroup index (groupTracks: LibraryTags NonEmptyList) =
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
            printfGray $"  <{duration} {extNoPeriod} {bitRate} {fileSize}>{String.nl}"

        let printHeader () =
            groupTracks
            |> NonEmptyList.head
            |> mainArtists ", "
            |> printfn "%d. %s" (index + 1) // Start numbering at 1, not 0.

        let printDuplicates () =
            groupTracks
            |> NonEmptyList.iter printFileSummary

        printHeader ()
        printDuplicates ()

    match groupedTracks with
    | None -> printfn "No duplicates found."
    | Some group -> group |> NonEmptyList.iteri printGroup
