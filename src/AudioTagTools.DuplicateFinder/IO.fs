module DuplicateFinder.IO

open Errors
open Settings
open Shared.TagLibrary
open Shared.IO
open CCFSharpUtils.Library
open System
open System.Text
open System.IO

let readFile (fileInfo: FileInfo) : Result<string, Error> =
    fileInfo |> readFile |! ReadFileError

let savePlaylist (settings: Settings) (tags: MultipleLibraryTags array option) : Result<unit, Error> =
    let now = DateTime.Now.ToString("yyyyMMdd_HHmmss")
    let filename = $"Duplicates by AudioTagTools - {now}.m3u"
    let fullPath = Path.Combine(settings.Playlist.SaveDirectory, filename)

    let appendFileEntry (builder: StringBuilder) (m: LibraryTags) : StringBuilder =
        let seconds = m.Duration.TotalSeconds
        let artist =
            m.Artists
            |> Array.append m.AlbumArtists
            |> String.concat "; "
        let artistWithTitle = $"{artist} - {m.Title}"
        let extInf = $"#EXTINF:{seconds},{artistWithTitle}"
        builder.AppendLine extInf |> ignore

        let fullPath = Path.Combine(m.DirectoryName, m.FileName)

        let updatedPath =
            match settings.Playlist.SearchPath, settings.Playlist.ReplacePath with
            | s, _ when s |> String.hasNoText -> fullPath
            | s, r -> fullPath.Replace(s, r)

        builder.AppendLine updatedPath

    match tags with
    | None -> Ok ()
    | Some tags ->
        tags
        |> Array.collect id
        |> Array.fold appendFileEntry (StringBuilder "#EXTM3U\n")
        |> _.ToString()
        |> writeTextToFile fullPath
        |. (fun _ -> printfn $"Created playlist file \"{fullPath}\".")
        |! WriteFileError
