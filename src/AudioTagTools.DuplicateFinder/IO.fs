module DuplicateFinder.IO

open Errors
open Settings
open Shared.Constants
open Shared.TagLibrary
open CCFSharpUtils
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

    let appendFileEntry (sb: StringBuilder) (tags: LibraryTags) : StringBuilder =
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

    tags
    |> List.collect id
    // |> Seq.concat
    |> List.fold appendFileEntry (StringBuilder "#EXTM3U\n")
    |> string
    |> File.writeText' file
    |! FileWriteError
    |. fun _ -> printfn $"Created playlist file \"{file}\"."
