module DuplicateFinder.IO

open Errors
open FSharpPlus.Data
open Settings
open Shared.Constants
open Shared.TagLibrary
open CCFSharpUtils
open System
open System.IO

let savePlaylist
    (settings: Settings)
    (tags: LibraryTags nlist nlist option)
    : Result<unit, DupeFinderError> =

    let now = DateTime.Now.ToString timeStampFormat
    let fileName = $"Duplicates by AudioTagTools - {now}.m3u"
    let file = FileInfo <| Path.Combine(settings.Playlist.SaveDirectory, fileName)

    let appendFileEntry (sb: SB) (tags: LibraryTags) : SB =
        let seconds = tags.Duration.TotalSeconds
        let artist =
            tags.Artists
            |> Array.append tags.AlbumArtists
            |> String.concat "; "
        let artistWithTitle = $"{artist} - {tags.Title}"
        let extInf = $"#EXTINF:{seconds},{artistWithTitle}"

        sb.AppendLine extInf |> ignore

        let filePath =
            let oldPath = Path.Combine(tags.DirectoryName, tags.FileName)
            match settings.Playlist.SearchPath,
                  settings.Playlist.ReplacePath with
            | s, _ when s |> String.hasNoText -> oldPath
            | s, r -> oldPath.Replace(s, r)

        sb.AppendLine filePath

    match tags with
    | None ->
        Ok ()
    | Some tags' ->
        tags'
        |> NonEmptyList.toList
        |> List.map NonEmptyList.toList
        |> List.concat
        |> List.fold appendFileEntry (SB $"#EXTM3U{String.nl}")
        |> string
        |> File.writeText' file
        |. fun _ -> printfn $"Created playlist file \"{file}\"."
        |! FileWriteError
