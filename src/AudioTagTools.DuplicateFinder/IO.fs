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

let savePlaylist (settings: Settings) (maybeTags: DuplicateTags option) : Result<unit, CommandError> =
    let makeFileInfo count =
        let now = DateTime.Now.ToString timeStampFormat
        let itemLabel = String.pluralizeSWithCount "item" count
        let fileName = $"Duplicates by AudioTagTools - {now} - {itemLabel}.m3u"
        FileInfo <| Path.Combine(settings.Playlist.SaveDirectory, fileName)

    /// Appends 2 lines to `sb` for the `tags`: a metadata summary and full file path.
    let appendFileData (sb: SB) (fileTags: LibraryTags) : SB =
        let seconds = fileTags.Duration.TotalSeconds
        let artist = fileTags.Artists |> Array.append fileTags.AlbumArtists |> String.concat "; "
        let artistWithTitle = $"{artist} - {fileTags.Title}"
        let extInf = $"#EXTINF:{seconds},{artistWithTitle}"

        sb.AppendLine extInf |> ignore

        let filePath =
            let originalPath = Path.Combine(fileTags.DirectoryName, fileTags.FileName)
            match settings.Playlist.SearchPath,
                  settings.Playlist.ReplacePath with
            | s, _ when String.hasNoText s -> originalPath
            | s, r -> originalPath.Replace(s, r)

        sb.AppendLine filePath

    match maybeTags with
    | None ->
        Ok ()
    | Some duplicateGroups ->
        let fileHeader = $"#EXTM3U{String.nl}"
        let newPlaylistFile = makeFileInfo duplicateGroups.Length
        duplicateGroups
        |> NonEmptyList.concat
        |> NonEmptyList.fold appendFileData (SB fileHeader)
        |> string
        |> File.writeText' newPlaylistFile
        |-- fun _ -> printfn $"Created playlist file \"{newPlaylistFile}\"."
        |!! FileWriteError
