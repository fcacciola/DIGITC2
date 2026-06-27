# Transgraphier 2.4.1 User Manual

Digital Instrumental Trans-Communication (ITC) workbench

## 1. Purpose

Transgraphier is an experimental Instrumental Trans-Communication (ITC) research tool.

In ordinary Electronic Voice Phenomena (EVP) work, an operator records noise or ambient audio and later listens for possible voice-like messages. Transgraphier explores a different and more constrained idea: if an unknown communicating agency can influence a noisy recording at all, perhaps the most robust signal it could imprint is not spoken voice, but a timed sequence of sound-energy bursts representing a text message.

The application therefore tries to extract an English message from an EVP-style recording by looking for burst patterns that can be interpreted as a tap-coded message.

This is a research tool. It does not prove that a message is paranormal, spiritual, or externally generated. Noise, artifacts, expectation, and ordinary signal-processing effects can all produce patterns. The app is designed to make the decoding attempt more objective and inspectable by showing the intermediate processing stages.

## 2. Simple Use

For normal use, the workflow is intentionally simple.

1. Open Transgraphier in the browser.
2. Upload an existing `.wav` file, or record audio directly in the app.
3. Press **Process**.
4. Wait for the result.
5. If a message is decoded, it appears at the top of the page.
6. If no message is decoded, try another EVP recording.

That is the basic intended workflow.

For many users, there is no need to inspect the waveforms or change parameters. The app is designed so a non-technical user can simply provide a recording and check whether a decoded message appears.

## 3. Recording Or Uploading Audio

### Upload

Use the file selector to choose a `.wav` file. If the file is valid, the input waveform appears immediately.

### Record

Use **Record** to capture audio through the browser microphone. Press **Stop** when finished. The recorded audio is immediately loaded into the app as the input waveform.

After recording, use **Download** if you want to save that exact recorded `.wav` file for later processing or archival.

## 4. Main Result

After processing, the overall result appears above the waveform views.

If a message is decoded, the result is shown as:

```text
Decoded Text Message:
...

Overall Fitness: ...
```

If no message can be decoded, the app shows:

```text
<<<< NO MESSAGE COULD BE DECODED >>>
```

This does not necessarily mean the recording has no interesting structure. It only means that this processing run did not produce a valid decoded message according to the current settings.

## 5. The Waveform Views

The app displays the input waveform and the intermediate processing results as wave views.

All wave views zoom and pan together. This makes it possible to compare the same time region across processing stages.

Use the mouse wheel to zoom. Click and drag a waveform to pan. The input waveform includes a time ruler.

The **Measure** tool allows click-drag selection on any waveform. It reports the selected interval in samples and time.

## 6. Block Navigation

When the decoder finds separator patterns in the color-coded tap-code signal, the app enables block navigation controls.

Use:

- **First Block**
- **Prev Block**
- **Next Block**

These controls zoom and pan the shared waveform view to one decoded block at a time.

A block is a time region defined by separator pulses in the tap-code interpretation. In practical terms, block navigation helps the researcher inspect each decoded letter or segment in context.

## 7. Advanced Use

Advanced users can inspect the processing steps and adjust parameters before re-processing.

The parameter table appears below the input waveform. Some parameters may initially be set to `-1`. In this app, `-1` usually means “let the system estimate this value statistically.”

After processing, the app may update the table with values calculated during the pipeline. If the researcher disagrees with a calculated value, they can edit it manually and press **Process** again.

## 8. Processing Overview

The processing pipeline tries to transform noisy audio into a discrete pulse sequence, then interpret that sequence as tap-coded text.

### 8.1 Noise Floor Gate

The first major step is a noise floor gate.

Before gating, the app calculates a smoothed envelope of the input. This is shown as the **First Envelope** wave view.

The noise floor gate removes parts of the signal considered to be below the usable sound-energy threshold. The result is shown as a waveform.

Parameter:

```text
Noise Floor
```

If this value is `-1`, the system calculates the noise floor automatically. After processing, the calculated value is shown in the parameter table.

If the user sees that the gate is too permissive or too aggressive, they can change this value manually and re-process.

### 8.2 Envelope And Upward Compression

The gated signal is then smoothed with an envelope step and processed with upward compression.

These steps help equalize levels so that weaker but potentially meaningful bursts can be compared more fairly with stronger ones.

The results are shown as waveform views.

### 8.3 Discretization

The signal is then converted into a square wave. This is shown in the **Discretized** wave view.

At this stage, nearby squares may be merged into a single event.

Parameter:

```text
Discretize Merge Prominence
```

This is a gauge-like value from `0` to `1`.

If too many events are being merged, lower or adjust this value and re-process. If events that should belong together remain separated, adjust it in the other direction and re-process.

### 8.4 Pulse Filtering

Each square-wave event is treated as a pulse.

The next step removes pulses that are too short to be considered reliable.

Parameter:

```text
Minimum Pulse Width
```

If this value is `-1`, the system estimates it statistically.

If short spurious pulses remain, increase the threshold. If valid-looking pulses disappear, lower it. Then press **Process** again.

The filtered pulse result is shown next to the discretized waveform.

### 8.5 Gap Classification

The app analyzes the gaps between pulses to classify them for tap-code counting.

After classification, there are two broad gap types:

- Intra-count gaps
- Larger gaps

Parameter:

```text
Intra Count Gap
```

If this value is `-1`, the system estimates it statistically.

The result is shown in a color-coded waveform. Pulses with a preceding gap classified as intra-count are shown in green. Pulses with a larger preceding gap are shown in yellow.

For example, a run like:

```text
Yellow - Green - Green - Green
```

is interpreted as one count of `4`.

If the color coding appears wrong, adjust **Intra Count Gap** and re-process.

### 8.6 Tap-Code Pair Detection

The classified pulses are then converted into tap-code pairs.

The next color-coded waveform shows the tap-code pair interpretation:

- Rows are shown in blue.
- Columns are shown in red.

The decoded message is expected to use a variation of tap code based on a Polybius-style square.

Letters are separated by a consecutive run of separator pulses. For a binary square, this separator is usually `5` or more pulses. Larger squares may require larger row/column counts, and therefore larger separators.

Separators are shown as black bars in the tap-coded color-coded waveform.

### 8.7 Timeline

The timeline view shows the letters decoded for each block.

Use the block navigation controls to step through these blocks when separators were detected.

## 9. Interpreting Results Carefully

Transgraphier is meant to support research, not replace judgment.

A decoded message should be treated as a candidate result, not as proof by itself. Researchers should consider:

- Whether the same result can be reproduced from the same recording.
- Whether small parameter changes produce stable or unstable messages.
- Whether the waveform stages visually support the decoded sequence.
- Whether the result could plausibly arise from noise, artifacts, or overfitting.
- Whether independent operators reach similar conclusions.

The most valuable results are those that remain stable under reasonable parameter changes and are supported by visible pulse structure in the intermediate waveforms.

## 10. Troubleshooting

### The App Says No Message Could Be Decoded

This is expected for many recordings. Try another recording, or inspect the advanced processing views.

### The Input Waveform Does Not Appear

Make sure the input file is a supported `.wav` file.

### The Server Cannot Be Reached

If running locally, make sure the server is running and the browser is using the correct URL.

In development mode, the usual client URL is:

```text
http://localhost:5173
```

In published mode, the usual app URL is:

```text
http://localhost:5188
```

### A Recording Works In The Browser But Does Not Process

The engine expects audio at `44100 Hz`. Browser-recorded files are converted to this format by the app, but externally recorded files should also use a standard WAV format.

### Parameter Changes Do Not Seem To Help

Some recordings may not contain a decodable pulse structure. Parameter tuning can improve borderline cases, but it cannot create reliable structure where none is present.

## 11. Notes On ITC And EVP

Electronic Voice Phenomena (EVP) are commonly described as apparent voice-like sounds found in recordings, often among noise or static, and interpreted by practitioners as possible communication. Instrumental Trans-Communication (ITC) is a broader term for attempts to communicate through electronic or technical devices.

The conventional scientific position is cautious or skeptical: many apparent EVP results may be explained by auditory pareidolia, expectation, interference, or recording artifacts. Transgraphier is built with that concern in mind by attempting to make the decoding process explicit, repeatable, and inspectable.

This manual does not ask the user to accept any particular explanation. It describes how to use the tool and how to inspect its output.
