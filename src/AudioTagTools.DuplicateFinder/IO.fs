module DuplicateFinder.IO

open Errors
open Settings
open Shared.Constants
open Shared.TagLibrary
open CCFSharpUtils
open FSharpPlus.Data
open System
open System.Text
open System.IO

let savePlaylist
    (settings: Settings)
    (tags: LibraryTags list list)
    : Result<unit, DupeFinderError> =

    let now = DateTime.Now.ToString timeStampFormat
    let fileName = $"Duplicates by AudioTagTools - {now}.m3u"
    let file = FileInfo <| Path.Combine(settings.Playlist.SaveDirectory, fileName)

    let appendFileEntry (sb: StringBuilder) (t: LibraryTags) : StringBuilder =
        let seconds = t.Duration.TotalSeconds
        let artist =
            t.Artists
            |> Array.append t.AlbumArtists
            |> String.concat "; "
        let artistWithTitle = $"{artist} - {t.Title}"
        let extInf = $"#EXTINF:{seconds},{artistWithTitle}"

        sb.AppendLine extInf |> ignore

        let filePath =
            let oldPath = Path.Combine(t.DirectoryName, t.FileName)
            match settings.Playlist.SearchPath,
                  settings.Playlist.ReplacePath with
            | s, _ when s |> String.hasNoText -> oldPath
            | s, r -> oldPath.Replace(s, r)

        sb.AppendLine filePath

    tags
    |> List.collect id
    // |> Seq.concat
    |> List.fold appendFileEntry (StringBuilder "#EXTM3U\n")
    |> string
    |> File.writeText' file
    |! FileWriteError
    |. fun _ -> printfn $"Created playlist file \"{file}\"."
