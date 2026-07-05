import { X } from "lucide-react";

type UserManualProps = {
  onClose: () => void;
};

export function UserManual({ onClose }: UserManualProps) {
  return (
    <div className="manual-backdrop" role="presentation" onMouseDown={onClose}>
      <section
        className="manual-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="manual-title"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <header className="manual-header">
          <div>
            <h2 id="manual-title">Transgraphier 2.4.1 User Manual</h2>
            <p>Digital Instrumental Trans-Communication (ITC) workbench</p>
          </div>
          <button type="button" className="manual-close" onClick={onClose} title="Close manual">
            <X size={18} aria-hidden="true" />
          </button>
        </header>

        <article className="manual-content">
          <section>
            <h3>Purpose</h3>
            <p>
              Transgraphier is an experimental Instrumental Trans-Communication (ITC) research tool. In ordinary
              Electronic Voice Phenomena (EVP) work, an operator records noise or ambient audio and later listens for
              possible voice-like messages.
            </p>
            <p>
              Transgraphier explores a more constrained idea: if an unknown communicating agency can influence a noisy
              recording at all, perhaps the most robust signal it could imprint is not spoken voice, but a timed
              sequence of sound-energy bursts representing a text message.
            </p>
            <p>
              This is a research tool. It does not prove that a message is paranormal, spiritual, or externally
              generated. Noise, artifacts, expectation, and ordinary signal-processing effects can all produce patterns.
            </p>
          </section>

          <section>
            <h3>Simple Use</h3>
            <ol>
              <li>Open Transgraphier in the browser.</li>
              <li>Upload an existing WAV file, or record audio directly in the app.</li>
              <li>Press <strong>Process</strong>.</li>
              <li>Wait for the result.</li>
              <li>If a message is decoded, it appears at the top of the page.</li>
              <li>If no message is decoded, try another EVP recording.</li>
            </ol>
            <p>
              For many users, there is no need to inspect waveforms or change parameters. The app is designed so a
              non-technical user can simply provide a recording and check whether a decoded message appears.
            </p>
          </section>

          <section>
            <h3>Recording Or Uploading Audio</h3>
            <p>
              Use the file selector to choose a WAV file. If the file is valid, the input waveform appears immediately.
            </p>
            <p>
              Use <strong>Record</strong> to capture audio through the browser microphone. Press <strong>Stop</strong>{" "}
              when finished. The recorded audio is immediately loaded as the input waveform.
            </p>
            <p>
              After recording, use <strong>Download</strong> to save that exact recorded WAV file for later processing
              or archival.
            </p>
          </section>

          <section>
            <h3>Main Result</h3>
            <p>After processing, the overall result appears above the waveform views.</p>
            <pre>{["Decoded Text Message:", "...", "", "Overall Fitness: ..."].join("\n")}</pre>
            <p>If no message can be decoded, the app shows:</p>
            <pre>{"<<<< NO MESSAGE COULD BE DECODED >>>"}</pre>
            <p>
              This does not necessarily mean the recording has no interesting structure. It only means that this
              processing run did not produce a valid decoded message according to the current settings.
            </p>
            <figure className="manual-figure">
              <img
                src="/manual/positive-result.png"
                alt="A positive Transgraphier processing result showing a decoded Hello World message, waveform stages, block navigation, and timeline letters."
              />
              <figcaption>
                A positive processing result. The decoded message appears at the top, followed by the input waveform,
                calculated parameters, intermediate processing waveforms, color-coded tap-code views, and the final
                timeline.
              </figcaption>
            </figure>
          </section>

          <section>
            <h3>Waveform Views</h3>
            <p>
              The app displays the input waveform and intermediate processing results as wave views. All wave views zoom
              and pan together, making it possible to compare the same time region across processing stages.
            </p>
            <p>
              Use the mouse wheel to zoom. Click and drag a waveform to pan. The input waveform includes a time ruler.
              The <strong>Measure</strong> tool allows click-drag selection on any waveform and reports the selected
              interval in samples and time.
            </p>
          </section>

          <section>
            <h3>Block Navigation</h3>
            <p>
              When the decoder finds separator patterns in the color-coded tap-code signal, the app enables block
              navigation controls: <strong>First Block</strong>, <strong>Prev Block</strong>, and{" "}
              <strong>Next Block</strong>.
            </p>
            <p>
              A block is a time region defined by separator pulses in the tap-code interpretation. Block navigation
              helps the researcher inspect each decoded letter or segment in context.
            </p>
          </section>

          <section>
            <h3>Advanced Use</h3>
            <p>
              Advanced users can inspect processing steps and adjust parameters before re-processing. Some parameters
              may initially be set to <code>-1</code>, which usually means "let the system estimate this value
              statistically."
            </p>
            <p>
              After processing, the app may update the table with values calculated during the pipeline. If the
              researcher disagrees with a calculated value, they can edit it manually and press <strong>Process</strong>{" "}
              again.
            </p>
          </section>

          <section>
            <h3>Processing Overview</h3>
            <h4>Noise Floor Gate</h4>
            <p>
              The first major step is a noise floor gate. Before gating, the app calculates a smoothed envelope of the
              input, shown as the <strong>First Envelope</strong> wave view. The <strong>Noise Floor</strong> parameter
              controls the threshold. A value of <code>-1</code> lets the system calculate it automatically.
            </p>

            <h4>Envelope And Upward Compression</h4>
            <p>
              The gated signal is smoothed and upward-compressed to equalize levels so weaker but potentially meaningful
              bursts can be compared more fairly with stronger ones.
            </p>

            <h4>Discretization</h4>
            <p>
              The signal is converted into a square wave. Nearby squares may be merged. The{" "}
              <strong>Discretize Merge Prominence</strong> parameter is a value from <code>0</code> to <code>1</code>{" "}
              controlling this behavior.
            </p>

            <h4>Pulse Filtering</h4>
            <p>
              Each square-wave event becomes a pulse. The <strong>Minimum Pulse Width</strong> parameter removes pulses
              that are too short. A value of <code>-1</code> lets the system estimate this statistically.
            </p>

            <h4>Gap Classification</h4>
            <p>
              The gaps between pulses are analyzed for tap-code counting. The <strong>Intra Count Gap</strong>{" "}
              parameter separates intra-count gaps from larger gaps. In the color-coded view, green pulses have a
              preceding intra-count gap, while yellow pulses have a larger preceding gap.
            </p>
            <pre>{"Yellow - Green - Green - Green = count 4"}</pre>

            <h4>Tap-Code Pair Detection</h4>
            <p>
              Classified pulses are converted into tap-code pairs. Rows are shown in blue and columns in red. Letter
              separators are shown as black bars. For a binary square, the separator is usually a run of 5 or more
              pulses; larger squares may require larger separators.
            </p>

            <h4>Timeline</h4>
            <p>
              The timeline view shows the letters decoded for each block. Use block navigation to step through these
              blocks when separators were detected.
            </p>
          </section>

          <section>
            <h3>Interpreting Results Carefully</h3>
            <p>A decoded message should be treated as a candidate result, not as proof by itself.</p>
            <ul>
              <li>Check whether the same result can be reproduced from the same recording.</li>
              <li>Check whether small parameter changes produce stable or unstable messages.</li>
              <li>Check whether the waveform stages visually support the decoded sequence.</li>
              <li>Consider whether the result could arise from noise, artifacts, or overfitting.</li>
              <li>When possible, compare interpretations from independent operators.</li>
            </ul>
          </section>

          <section>
            <h3>Troubleshooting</h3>
            <h4>No Message Could Be Decoded</h4>
            <p>This is expected for many recordings. Try another recording, or inspect the advanced processing views.</p>

            <h4>The Input Waveform Does Not Appear</h4>
            <p>Make sure the input file is a supported WAV file.</p>

            <h4>The Server Cannot Be Reached</h4>
            <p>
              In development mode, the usual client URL is <code>http://localhost:5173</code>. In published mode, the
              usual app URL is <code>http://localhost:5188</code>.
            </p>

            <h4>Parameter Changes Do Not Seem To Help</h4>
            <p>
              Some recordings may not contain a decodable pulse structure. Parameter tuning can improve borderline
              cases, but it cannot create reliable structure where none is present.
            </p>
          </section>
        </article>
      </section>
    </div>
  );
}
