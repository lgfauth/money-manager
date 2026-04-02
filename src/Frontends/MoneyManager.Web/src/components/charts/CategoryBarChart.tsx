'use client';

import { useEffect, useState } from 'react';

interface CategoryBarChartProps {
  data: {
    name: string;
    color: string;
    value: number;
  }[];
}

const formatCurrency = (value: number) =>
  `R$ ${value.toLocaleString('pt-BR', { minimumFractionDigits: 0 })}`;

export function CategoryBarChart({ data }: CategoryBarChartProps) {
  const [animated, setAnimated] = useState(false);
  const max = Math.max(...data.map((item) => item.value), 0);

  useEffect(() => {
    setAnimated(false);

    const timers = data.map((_, index) =>
      window.setTimeout(() => {
        setAnimated(true);
      }, index * 25)
    );

    return () => {
      timers.forEach((timer) => window.clearTimeout(timer));
    };
  }, [data]);

  if (data.length === 0) {
    return <p className="py-8 text-center text-sm text-muted-foreground">Sem dados no mês selecionado.</p>;
  }

  return (
    <div className="space-y-1">
      {data.map((item) => {
        const widthPercent = max > 0 ? (item.value / max) * 100 : 0;

        return (
          <div
            key={item.name}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 10,
              padding: '4px 0',
              borderRadius: 6,
              cursor: 'default',
              transition: 'background 0.15s',
            }}
            onMouseEnter={(event) => {
              event.currentTarget.style.background = '#f8f9fc';
            }}
            onMouseLeave={(event) => {
              event.currentTarget.style.background = 'transparent';
            }}
          >
            <span
              style={{
                width: 160,
                fontSize: 13,
                color: '#4a5568',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
              }}
              title={item.name}
            >
              {item.name}
            </span>

            <div
              style={{
                flex: 1,
                height: 10,
                background: '#f0f2f8',
                borderRadius: 99,
                overflow: 'hidden',
              }}
            >
              <div
                style={{
                  width: `${animated ? widthPercent : 0}%`,
                  background: item.color,
                  height: '100%',
                  borderRadius: 99,
                  transition: 'width 0.4s ease',
                }}
              />
            </div>

            <span
              style={{
                width: 90,
                textAlign: 'right',
                fontSize: 12,
                fontWeight: 600,
              }}
            >
              {formatCurrency(item.value)}
            </span>
          </div>
        );
      })}
    </div>
  );
}
