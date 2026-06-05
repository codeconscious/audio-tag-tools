module DuplicateFinder.Tags

open Errors
open Settings
open Shared
open Shared.TagLibrary
open FSharpPlus
open FSharpPlus.Data
open CCFSharpUtils
open CCFSharpUtils.Collections
open CCFSharpUtils.Operators
open CCFSharpUtils.Text
open System
open System.IO
open System.Text.RegularExpressions

let parseToTags json =
    json |> parseJsonToNonEmptyTags |!! TagParseError

let printCount description (tags: LibraryTags nlist) =
    printfn $"%s{description}%s{String.formatInt tags.Length}"

/// Filters out tags containing artists or titles specified in the exclusion patterns.
let discardExcluded (settings: Settings) (allTags: LibraryTags nlist)
    : Result<LibraryTags nlist, CommandError> =

    let matchOptions = RegexOptions.IgnoreCase

    let isExcluded tags =
        let (|ArtistAndTitle|ArtistOnly|TitleOnly|Invalid|) (pattern: ExclusionPattern) =
            match pattern.Artist, pattern.Title with
            | Some a, Some t -> ArtistAndTitle (a, t)
            | Some a, None   -> ArtistOnly a
            | None,   Some t -> TitleOnly t
            | _ -> Invalid

        let containsArtist artistPattern =
            [| tags.AlbumArtists; tags.Artists |]
            |> Array.concat
            |> Array.exists (fun artist -> Regex.IsMatch(artist, artistPattern, matchOptions))

        let titleStartsWith pattern = Regex.IsMatch(tags.Title, pattern, matchOptions)

        let checkIfExcluded = function
            | ArtistAndTitle (a, t) -> containsArtist a && titleStartsWith t
            | ArtistOnly a -> containsArtist a
            | TitleOnly t -> titleStartsWith t
            | Invalid -> false

        settings.ExclusionPatterns |> Array.exists checkIfExcluded

    allTags
    |> NonEmptyList.tryFilter (not << isExcluded)
    |> Option.toResultWith NoFilesRemainAfterFiltering

/// Returns a normalized string of two concatenated items:
/// (1) The artist name that should be used for artist grouping when searching for duplicates.
///     If the artist appears in any "equivalent artist" group, then the first equivalent artist
///     name from that group will be prioritized over the track's artist name.
/// (2) The track title.
/// The string is intended to be used solely for track grouping.
let private sanitizedTrackGroupingName (settings: Settings) fileTags =
    let scrubText patterns =
        Rgx.scrubMatches patterns
        >> String.stripWhiteSpace
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
        |> scrubText settings.ArtistReplacementPatterns

    let title = fileTags.Title |> scrubText settings.TitleReplacementPatterns

    $"{artist}{title}".ToLowerInvariant()

let findDuplicates settings tags : DuplicateTags option =
    monad {
        let! filtered = tags |> NonEmptyList.tryFilter hasArtistAndTitle

        let! groupedDupes =
            filtered
            |> NonEmptyList.groupBy (sanitizedTrackGroupingName settings)
            |> NonEmptyList.tryFilter (snd >> NonEmptyList.hasMultiple)

        return
            groupedDupes
            |> sortBy fst
            |> NonEmptyList.map (snd >> sortBy _.Title.Length)
    }

let printDuplicates (groupedTracks: DuplicateTags option) : unit =
    let printfGray = printfColor ConsoleColor.DarkGray

    let printGroup index (tracks: LibraryTags nlist) =
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
            tracks
            |> NonEmptyList.head
            |> mainArtists ", "
            |> printfn "%d. %s" (index + 1) // Start numbering at 1, not 0.

        let printDuplicates () =
            tracks
            |> NonEmptyList.iter printFileSummary

        printHeader ()
        printDuplicates ()

    match groupedTracks with
    | None -> printfn "No duplicates found."
    | Some group -> group |> NonEmptyList.iteri printGroup
