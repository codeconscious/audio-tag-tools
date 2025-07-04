module TagLibrary

open FSharp.Data

[<Literal>]
let private tagSample = """
[
  {
    "FileNameOnly": "text",
    "DirectoryName": "text",
    "Artists": ["text"],
    "AlbumArtists": ["text"],
    "Album": "text",
    "TrackNo": 0,
    "Title": "text",
    "Year": 0,
    "Genres": ["text"],
    "Duration": "00:00:00",
    "LastWriteTime": "2023-09-13T13:49:44+09:00"
  }
]"""

type TagJsonProvider = JsonProvider<tagSample>
type FileTags = TagJsonProvider.Root
type FileTagCollection = FileTags array
type FilteredTagCollection = FileTags array

let parseToTags (json: string) : Result<FileTagCollection, string> =
    try Ok (TagJsonProvider.Parse json)
    with ex -> Error ex.Message
