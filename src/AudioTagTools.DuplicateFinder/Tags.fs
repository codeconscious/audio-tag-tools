module Tags

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
    let excludeFile (settings: SettingsRoot) (tags: LibraryTags) : bool =
        let isExcluded (exclusion: SettingsProvider.Exclusion) : bool =
            match exclusion.Artist, exclusion.Title with
            | Some a, Some t ->
                anyContains [tags.AlbumArtists; tags.Artists] a &&
                tags.Title.StartsWith(t, StringComparison.InvariantCultureIgnoreCase)
            | Some a, None ->
                anyContains [tags.AlbumArtists; tags.Artists] a
            | None, Some t ->
                tags.Title.StartsWith(t, StringComparison.InvariantCultureIgnoreCase)
            | _ -> false

        settings.Exclusions
        |> Array.exists isExcluded

    allTags
    |> Array.filter (not << excludeFile settings)

let private hasArtistOrTitle track =
    let hasAnyArtist (track: LibraryTags) =
        track.Artists.Length > 0 || track.AlbumArtists.Length > 0

    let hasTitle (track: LibraryTags) =
        not <| String.IsNullOrWhiteSpace track.Title

    hasAnyArtist track && hasTitle track

let private mainArtists (separator: string) (track: LibraryTags) =
    match track with
    | t when t.AlbumArtists.Length > 0
             && t.AlbumArtists[0] <> String.Empty
             && t.AlbumArtists[0] <> "Various"
             && t.AlbumArtists[0] <> "Various Artists"
             && t.AlbumArtists[0] <> "Multiple Artists" ->
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
        |> mainArtists String.Empty
        |> removeSubstrings settings.ArtistReplacements

    let title =
        track.Title
        |> removeSubstrings settings.TitleReplacements

    $"{artists}{title}"
        .Normalize(NormalizationForm.FormC)
        .ToLowerInvariant()

let findDuplicates (settings: SettingsRoot) (tags: LibraryTags array) : LibraryTags array array option =
    tags
    |> Array.filter hasArtistOrTitle
    |> Array.groupBy (groupName settings)
    |> Array.sortBy fst
    |> Array.map snd
    |> Array.filter (fun groupedTracks -> groupedTracks.Length > 1)
    |> function [||] -> None | tagData -> Some tagData

let printTotalCount (tags: LibraryTags array) =
    printfn $"Total file count:    %s{formatNumber tags.Length}"

let printFilteredCount (tags: LibraryTags array) =
    printfn $"Filtered file count: %s{formatNumber tags.Length}"

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
            else $"""{String.Join(", ", track.Artists)}  /  """

        // Print each suspected duplicate track in the group.
        groupTracks
        |> Array.iter (fun x -> printfn $"""    • {artistText x}{x.Title}""")

    match groupedTracks with
    | None -> printfn "No duplicates found."
    | Some gt -> gt |> Array.iteri printGroup
