module DuplicateFinder.Settings

open Errors
open FSharp.Data

[<Literal>]
let settingsSample = """
{
    "playlist": {
        "saveDirectory": "path",
        "searchPath": "path",
        "replacePath": "path"
    },
    "exclusions": [
        {
            "artist": "artist"
        },
        {
            "title": "title"
        },
        {
            "artist": "artist",
            "title": "title"
        }
    ],
    "equivalentArtists": [
        ["artistName", "equivalentArtistName"]
    ],
    "artistReplacements": [
        "text"
    ],
    "titleReplacements": [
        "text"
    ]
}
"""

type SettingsProvider = JsonProvider<settingsSample>
type Settings = SettingsProvider.Root
type Exclusion = SettingsProvider.Exclusion

let parseToSettings (json: string) : Result<Settings, Error> =
    try
        json
        |> SettingsProvider.Parse
        |> Ok
    with
    | e -> Error (SettingsParseError e.Message)

let printSummary (settings: Settings) =
    printfn "Settings:"
    printfn $"  Exclusions:          %d{settings.Exclusions.Length}"
    printfn $"  Artist Replacements: %d{settings.ArtistReplacements.Length}"
    printfn $"  Title Replacements:  %d{settings.TitleReplacements.Length}"
