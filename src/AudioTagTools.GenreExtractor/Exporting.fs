module GenreExtractor.Exporting

open Errors
open Shared.TagLibrary
open CCFSharpUtils
open CCFSharpUtils.Collections
open CCFSharpUtils.Text
open FSharpPlus
open FSharpPlus.Data
open System
open Shared

let private mainArtist (fileTags: LibraryTags) : Artist option =
    let hasValidValue xs = Array.isNotEmpty xs && String.hasText xs[0]

    match fileTags with
    | a when a.Artists      |> hasValidValue -> Some (Artist a.Artists[0])
    | a when a.AlbumArtists |> hasValidValue -> Some (Artist a.AlbumArtists[0])
    | _ -> None

let printOldSummary (oldGenres: string list) : unit =
    match oldGenres with
    | [] ->
        printfn "No existing genre data found."
    | _  ->
        let count = oldGenres.Length
        printfn "%s %s in the old file."
            (String.formatInt count)
            (String.pluralize "entry" "entries" count)

let printTagCount (tags: LibraryTags nlist) =
    printfn $"Parsed tags for {String.formatInt tags.Length} files from the tag library."

let private allGenres (fileTags: LibraryTags nlist) : string list =
    fileTags
    |> NonEmptyList.tryCollect (fun t -> t.Genres |> toList)
    |> function None -> [] | Some gs -> gs |> toList

let private mostCommon (xs: string list) : string option =
    match xs with
    | [] -> None
    | _  -> Some (xs |> List.filter String.hasText |> groupBy id |> maxBy (snd >> length) |> fst)

let private mostCommonGenre = allGenres >> mostCommon

let generateGenreData (separator: string) (allFileTags: LibraryTags nlist)
    : Result<string nlist, CommandError> =

    allFileTags
    |> NonEmptyList.groupBy mainArtist
    |> NonEmptyList.tryChoose (fun (artistOpt, tagGroup) ->
        match artistOpt with
        | None -> None
        | Some (Artist artist) ->
            tagGroup |> mostCommonGenre |> map (fun genre -> $"{artist}{separator}{genre}"))
    |> Option.map NonEmptyList.sort
    |> Option.toResultWith InsufficientGenreData

let printChanges (oldGenres: string list) (newGenres: string nlist) =
    let newTotalCount = newGenres.Length
    let addedCount = newGenres |> NonEmptyList.toList |> List.except oldGenres |> _.Length
    let deletedCount = oldGenres |> List.except newGenres |> _.Length
    printfn "Prepared %s artist-genre entries total (%s new, %s deleted)."
        (String.formatInt newTotalCount)
        (String.formatInt addedCount)
        (String.formatInt deletedCount)
