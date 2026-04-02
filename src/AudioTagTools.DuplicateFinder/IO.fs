module DuplicateFinder.IO

open Errors
open Settings
open Shared.Constants
open Shared.TagLibrary
open CCFSharpUtils.Library
open FSharpPlus.Data
open System
open System.Text
open System.IO

let savePlaylist
    (settings: Settings)
    (tags: LibraryTags array NonEmptyList option)
    : Result<unit, DupeFinderError> =

    let now = DateTime.Now.ToString timeStampFormat
    let fileName = $"Duplicates by AudioTagTools - {now}.m3u"
    let file = FileInfo <| Path.Combine(settings.Playlist.SaveDirectory, fileName)

    let appendFileEntry (builder: StringBuilder) (m: LibraryTags) : StringBuilder =
        let seconds = m.Duration.TotalSeconds
        let artist =
            m.Artists
            |> Array.append m.AlbumArtists
            |> String.concat "; "
        let artistWithTitle = $"{artist} - {m.Title}"
        let extInf = $"#EXTINF:{seconds},{artistWithTitle}"
        builder.AppendLine extInf |> ignore

        let oldPath = Path.Combine(m.DirectoryName, m.FileName)

        let savePath =
            match settings.Playlist.SearchPath,
                  settings.Playlist.ReplacePath with
            | s, _ when s |> String.hasNoText -> oldPath
            | s, r -> oldPath.Replace(s, r)

        builder.AppendLine savePath

    match tags with
    | None -> Ok ()
    | Some tags ->
        tags
        |> NonEmptyList.gather id
        |> Seq.concat
        |> Seq.fold appendFileEntry (StringBuilder "#EXTM3U\n")
        |> string
        |> File.writeText' file
        |! FileWriteError
        |. fun _ -> printfn $"Created playlist file \"{file}\"."
