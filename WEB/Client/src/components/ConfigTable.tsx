import type { ConfigParam } from "../types";

type ConfigTableProps = {
  params: ConfigParam[];
  disabled: boolean;
  onChange: (params: ConfigParam[]) => void;
};

export function ConfigTable({ params, disabled, onChange }: ConfigTableProps) {
  if (params.length === 0) {
    return null;
  }

  return (
    <section className="config-table" aria-label="Processing configuration">
      <div className="config-row config-label-row">
        {params.map((param) => (
          <div key={param.name} className="config-cell config-label" title={param.name}>
            {param.label}
          </div>
        ))}
      </div>
      <div className="config-row">
        {params.map((param, index) => (
          <label key={param.name} className="config-cell">
            <input
              value={param.value}
              disabled={disabled}
              onChange={(event) => {
                const next = [...params];
                next[index] = { ...param, value: event.target.value };
                onChange(next);
              }}
            />
          </label>
        ))}
      </div>
    </section>
  );
}
