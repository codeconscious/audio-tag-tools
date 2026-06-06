module DuplicateFinder.IO

open Errors
open Settings
open Shared.Constants
open Shared.TagLibrary
open CCFSharpUtils
open CCFSharpUtils.IO
open CCFSharpUtils.Operators
open CCFSharpUtils.Text
open FSharpPlus.Data
open System
open System.IO

let savePlaylist
    (settings: Settings)
    (maybeTags: DuplicateTags option)
    : Result<unit, CommandError> =

    let makeFileInfo count =
        let now = DateTime.Now.ToString timeStampFormat
        let itemLabel = String.pluralizeSWithCount "item" count
        let fileName = $"Duplicates by AudioTagTools - {now} - {itemLabel}.m3u"
        FileInfo <| Path.Combine(settings.Playlist.SaveDirectory, fileName)

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
            | s, _ when String.hasNoText s -> oldPath
            | s, r -> oldPath.Replace(s, r)

        sb.AppendLine filePath

    match maybeTags with
    | None ->
        Ok ()
    | Some duplicateGroups ->
        let fileInfo = makeFileInfo duplicateGroups.Length
        duplicateGroups
        |> NonEmptyList.concat
        |> NonEmptyList.fold appendFileEntry (SB $"#EXTM3U{String.nl}")
        |> string
        |> File.writeText' fileInfo
        |-- fun _ -> printfn $"Created playlist file \"{fileInfo}\"."
        |!! FileWriteError
