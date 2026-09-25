module Cacher.IO

open Errors
open CCFSharpUtils
open CCFSharpUtils.Text
open CCFSharpUtils.Collections
open System.IO

let private supportedAudioExtensions =
    // Supported file format extensions from https://github.com/mono/taglib-sharp.
    [ ".aa"; ".aax"; ".aac"; ".aiff"; ".ape"; ".dsf"; ".flac"; ".m4a"; ".m4b"; "m4p"
      ".mp3"; ".mpc"; ".mpp"; ".ogg"; ".oga"; ".wav"; ".wma"; ".wv"; ".webm"

      // Additional custom extensions:
      ".mp4"; ".opus" ]

let private isSupportedAudioFile (fileInfo: FileInfo) =
    supportedAudioExtensions
    |> List.exists (String.equalIgnoreCase fileInfo.Extension)

let getFileInfos (dir: DirectoryInfo) : Result<FileInfo nseq, CommandError> =
    try
        dir.EnumerateFiles("*", SearchOption.AllDirectories)
        |> Seq.filter isSupportedAudioFile
        |> Seq.toNonEmptySeqResult (NoFilesFound dir.FullName)
    with
    | exn -> Error (IoError exn.Message)
