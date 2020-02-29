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

FIT, GPX, and TCX (which borrows from GPX) use different words, but they map to common concepts. Dauer exploits these common abstractions.

### Concept Map

|Dauer Concept |FIT          |TCX                    |GPX        |Description                                                            |
|---           |---          |---                    |---        |---                                                                    |
|Sequence      |Session, Lap |Activity, Lap, Track   |Track      |Temporally sequential, discrete stream of data with a start and end. Sequences can contain sequences, e.g. TCX Activity contains Laps which contain Track, and FIT Sessions contains Laps. |
|Sample        |Record       |Trackpoint             |Trackpoint |Group of data associated with a moment in time, e.g. speed             |

## File Formats

The FIT file format is binary: compact and suited for low-power wearables, but not human-readable as it is not plaintext; viewing the data requires a specialized parser. TCX and GPX are XML, which is arguably readable but heavyweight, complete with XML schemas and namespaces. Dauer seeks both portability and readability by using JSON as its data interchange format.

|Dauer  |FIT    |TCX  |GPX
|---    |---    |---  |---  
|JSON   |Binary |XML  |XML

## File Contents

A Dauer JSON object is a hierarchy of sequences. At the bottom of the hierarchy are samples. Samples contain actual data such as speed, distance, or position. The depth of the hierarchy depends on the data source.

FIT files have the following structure:

|Name        |Type      |Discussion
|---         |---       |---
|Session     |Sequence  |Laps are mapped to a session by start timestamps and duration.
|Lap         |Sequence  |Records are mapped to a lap by start timestamp and duration.
|Record      |Sample    |Bottom of the hierarchy, where temporal data lives.

Corresponding Dauer JSON:

```
{
    // Sessions
    "sequences": [

        // Laps
        "sequences": [

            // Records
            "samples": [
                {
                    "when": "2020-02-06T01:10:39"
                    "speed": 0.5,
                    "distance": 1.57
                }, 
                ...
            ]
        ]
    ]
}
```

TCX files have the following structure:

|Name        |Type      |Discussion
|---         |---       |---
|Activity    |Sequence  |Laps are contained within the Activity XML element.
|Lap         |Sequence  |Trackpoints are contained within the Track XML element. Each Lap has one Track.
|Trackpoint  |Sample    |Bottom of the hierarchy, where temporal data lives.


Corresponding Dauer JSON:

```
{
    // Activities
    "sequences": [

        // Laps
        "sequences": [
            
            // Trackpoints
            "samples": [
                {
                    "when": "2020-02-06T01:10:39"
                    "speed": 0.5,
                    "distance": 1.57
                }, 
                ...
            ]
        ]
    ]
}
```

## Why Dauer?

*Dauer* is a German word with several meanings. In one sense it refers to duration, and in another it refers to endurance. It seems a good name for a project focused on duration-orientated data for endurance sports. 

The author of this software is not German, but has an enthusiasm for the language and places that use it.