module Tags

open System.IO
open Errors
open Utilities
open Settings
open Operators
open TagLibrary
open System
open System.Text

let parseToTags json =
    parseJsonToTags json
    |> Result.mapError TagParseError

let filter (settings: SettingsRoot) (allTags: LibraryTags array) : LibraryTags array =
    let isExcluded (tags: LibraryTags) =
        settings.Exclusions
        |> Array.exists (fun excl ->
            match excl.Artist, excl.Title with
            | Some artist, Some title ->
                Array.contains artist tags.AlbumArtists ||
                Array.contains artist tags.Artists ||
                tags.Title.StartsWith(title, StringComparison.InvariantCultureIgnoreCase)
            | Some artist, None ->
                Array.contains artist tags.AlbumArtists ||
                Array.contains artist tags.Artists
            | None, Some title ->
                tags.Title.StartsWith(title, StringComparison.InvariantCultureIgnoreCase)
            | _ -> false)

    allTags
    |> Array.filter (not << isExcluded)

let private hasArtistOrTitle track =
    let hasAnyArtist (track: LibraryTags) =
        track.Artists.Length > 0 ||
        track.AlbumArtists.Length > 0

    let hasTitle (track: LibraryTags) =
        not (String.IsNullOrWhiteSpace track.Title)

    hasAnyArtist track && hasTitle track

let private mainArtists (separator: string) (track: LibraryTags) =
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

let private groupName (settings: SettingsRoot) (track: LibraryTags) =
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
        $"{artist}{firstFile.Title}"

    groupedTags
    |> Array.sortBy artistAndTrackName

let findDuplicates (settings: SettingsRoot) (tags: LibraryTags array) : LibraryTags array array option =
    tags
    |> Array.filter hasArtistOrTitle
    |> Array.groupBy (groupName settings)
    |> Array.choose (fun (_, group) ->
        match group with
        | [| _ |] -> None // Filter out items with no potential duplicates.
        | duplicates -> Some duplicates)
    |> function [||] -> None | duplicates -> Some duplicates
    |> Option.map sortByArtist

let printTotalCount (tags: LibraryTags array) =
    printfn $"  Total file count:    %s{formatInt tags.Length}"

let printFilteredCount (tags: LibraryTags array) =
    printfn $"  Filtered file count: %s{formatInt tags.Length}"

let printResults (groupedTracks: LibraryTags array array option) =
    let printGroup index (groupTracks: LibraryTags array) =
        // Print the joined artists from this group's first file.
        groupTracks
        |> Array.head
        |> mainArtists ", "
        |> printfn "%d. %s" (index + 1) // Start at 1, not 0.

        let artistText (track: LibraryTags) =
            if Array.isEmpty track.Artists
            then String.Empty
            else $"""{String.Join(", ", track.Artists)}"""

        // Print each suspected duplicate track in the group.
        groupTracks
        |> Array.iter (fun fileTags ->
            let artist = artistText fileTags
            let title = fileTags.Title
            let duration = formatTimeSpan fileTags.Duration
            let extension = (Path.GetExtension fileTags.FileName)[1..] |> _.ToUpperInvariant()
            let bitrate = $"{fileTags.BitRate}kbps"
            let fileSize = formatBytes fileTags.FileSize
            printf $"""    • {artist}"""
            Console.ForegroundColor <- ConsoleColor.DarkGray
            printf " — "
            Console.ResetColor()
            printf $"{title}"
            Console.ForegroundColor <- ConsoleColor.DarkGray
            printfn $"""  [{duration}; {extension}; {bitrate}; {fileSize}]"""
            Console.ResetColor())

    match groupedTracks with
    | None -> printfn "No duplicates found."
    | Some gt -> gt |> Array.iteri printGroup
