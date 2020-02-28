# Dauer

Garmin FIT and TCX file manipulation with .NET.

## What can Dauer do?

Currently:

* Load and save TCX files
* Load and save Garmin FIT files

Roadmap:

* Edit TCX and FIT files
    * Convert between FIT and TCX
    * Merge, split, trim, splice
    * Edit temporal data
        * e.g. correct noisy and inaccurate treadmill pace

* Be a go-to .NET framework for editing fitness data.
    * Web app and API
    * Android and iOS apps
    * Integration with Garmin Connect and Strava

## Concepts

FIT, GPX, and TCX (which borrows from GPX) use different words, but they map to common concepts. Dauer abstracts each common concept with a new term.

### Concept Map

|Dauer Concept |FIT        |TCX        |GPX        |Description                                                           |
|---           |---        |---        |---        |---                                                                   |
|Workout       |Session    |Activity   |---        |Physical container of fitness data such as a file                     |
|Sequence      |Lap        |Lap, Track |Track      |Temporally sequential, discrete stream of data with a start and end |
|Sample        |Record     |Trackpoint |Trackpoint |Unit of data associated with a moment in time                         |

## File Formats

The FIT file format is binary: compact and suited for low-power wearables, but not human-readable as it is not plaintext; viewing the data requires a specialized parser. TCX and GPX are XML, which is arguably readable but heavyweight, complete with XML schemas and namespaces. Dauer seeks both portability and readability by using JSON as its data interchange format.

|Dauer  |FIT    |TCX  |GPX
|---    |---    |---  |---  
|JSON   |Binary |XML  |XML

## Why Dauer?

*Dauer* is an interesting German word with several meanings. In one sense it refers to duration, and in another it refers to endurance. It seems a fitting name for a project focused on duration-orientated data for endurance sports. 

The author of this software is not German, but has an enthusiasm for the language and places that use it.