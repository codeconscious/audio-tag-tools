module Tags

open System.IO
open Errors
open Settings
open TagLibrary
open System
open System.Text

let parseToTags json =
    parseJsonToTags json
    |> Result.mapError TagParseError

let filter (settings: SettingsRoot) (allTags: LibraryTags array) : LibraryTags array =
    let(|ArtistAndTitle|ArtistOnly|TitleOnly|Invalid|) (exclusion: Exclusion) =
        match exclusion.Artist, exclusion.Title with
        | Some a, Some t -> ArtistAndTitle (a, t)
        | Some a, None -> ArtistOnly a
        | None, Some t -> TitleOnly t
        | _ -> Invalid

    let isExcluded (tags: LibraryTags) =
        settings.Exclusions
        |> Array.exists (fun exclusion ->
            match exclusion with
            | ArtistAndTitle (artist, title) ->
                Array.contains artist tags.AlbumArtists ||
                Array.contains artist tags.Artists ||
                tags.Title.StartsWith(title, StringComparison.InvariantCultureIgnoreCase)
            | ArtistOnly artist ->
                Array.contains artist tags.AlbumArtists ||
                Array.contains artist tags.Artists
            | TitleOnly title ->
                tags.Title.StartsWith(title, StringComparison.InvariantCultureIgnoreCase)
            | Invalid -> false)

    allTags
    |> Array.filter (not << isExcluded)

let private hasArtistOrTitle track =
    let hasAnyArtist (track: LibraryTags) =
        track.Artists.Length > 0 ||
        track.AlbumArtists.Length > 0

    let hasTitle (track: LibraryTags) =
        not (String.IsNullOrWhiteSpace track.Title)

    hasAnyArtist track && hasTitle track

let private mainArtists (separator: string) (track: LibraryTags) : string =
    let forbiddenArtistNames =
        [
            String.Empty
            "Various"
            "Various Artists"
            "Multiple Artists"
            "\u003Cunknown\u003E"
        ]

    let noForbiddenAlbumArtists artist =
        forbiddenArtistNames
        |> List.exists _.Equals(artist, StringComparison.InvariantCultureIgnoreCase)
        |> not

    match track with
    | t when t.AlbumArtists.Length > 0 && noForbiddenAlbumArtists t.AlbumArtists[0] ->
        t.AlbumArtists
    | t ->
        t.Artists
    |> String.concat separator

let private groupingName (settings: SettingsRoot) (track: LibraryTags) =
    // It appears JSON type providers do not import whitespace-only values. Whitespace should
    // always be ignored to increase the accuracy of duplicate checks, so they are added here.
    let removeSubstrings arr =
        arr
        |> Array.append [| " "; "　" |] // Single-byte and double-byte spaces.
        |> removeSubstrings

    let artists =
        track
        |> mainArtists String.Empty // Separator unneeded since this text is for grouping only.
        |> removeSubstrings settings.ArtistReplacements

    let title =
        track.Title
        |> removeSubstrings settings.TitleReplacements

    $"{artists}{title}"
        .Normalize(NormalizationForm.FormC)
        .ToLowerInvariant()

let private sortByArtist (groupedTags: LibraryTags array array) =
    let artistAndTrackName (group: LibraryTags array) =
        let firstFile = group[0]
        let artist =
            if firstFile.AlbumArtists.Length > 1
            then firstFile.AlbumArtists[0]
            else firstFile.Artists[0]
        $"{artist}{firstFile.Title}" // Only used for sorting, so spaces aren't needed.

    groupedTags
    |> Array.sortBy artistAndTrackName

let findDuplicates (settings: SettingsRoot) (tags: LibraryTags array) : LibraryTags array array option =
    tags
    |> Array.filter hasArtistOrTitle
    |> Array.groupBy (groupingName settings)
    |> Array.choose (fun (_, groupedItems) ->
        match groupedItems with
        | [| _ |] -> None // Remove items with no potential duplicates.
        | duplicates -> Some duplicates)
    |> function [||] -> None | duplicates -> Some duplicates
    |> Option.map sortByArtist

let printCount description (tags: LibraryTags array) =
    printfn $"%s{description}%s{formatInt tags.Length}"

let printDuplicates (groupedTracks: LibraryTags array array option) =
    let printfGray = Printing.printfColor ConsoleColor.DarkGray

    let printGroup index (groupTracks: LibraryTags array) =
        let artistSummary (track: LibraryTags) : string =
            if Array.isEmpty track.Artists
            then String.Empty
            else String.Join(", ", track.Artists)

        let printFileSummary fileTags =
            let artist = artistSummary fileTags
            let title = fileTags.Title
            let duration = formatTimeSpan fileTags.Duration
            let periodlessExtension = (Path.GetExtension fileTags.FileName)[1..] |> _.ToUpperInvariant()
            let bitrate = $"{fileTags.BitRate}kbps"
            let fileSize = formatBytes fileTags.FileSize
            printf $"    • {artist}"
            printfGray " — "
            printf $"{title}"
            printfGray $"  [{duration} {periodlessExtension} {bitrate} {fileSize}]{Environment.NewLine}"

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
