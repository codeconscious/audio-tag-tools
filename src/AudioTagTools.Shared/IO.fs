module Shared.IO

open CCFSharpUtils
open System.IO

let backUpFile : FileInfo -> Result<FileInfo,string> =
    File.backUpWithTimestamp timeStampFormat
