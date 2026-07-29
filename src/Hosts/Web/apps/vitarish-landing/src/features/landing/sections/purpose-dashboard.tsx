"use client";

import { AreaChart, Widget } from "@heroui-pro/react";

export function PurposeDashboard({ fa }: { fa: boolean }) {
  const data = [
    { month: fa ? "ایده" : "Idea", idea: 0, purpose: 8 },
    { month: fa ? "کشف" : "Discover", idea: 31, purpose: 16 },
    { month: fa ? "شکل" : "Shape", idea: 46, purpose: 29 },
    { month: fa ? "آزمون" : "Test", idea: 62, purpose: 48 },
    { month: fa ? "ساخت" : "Build", idea: 78, purpose: 71 },
    { month: fa ? "هدف" : "Purpose", idea: 100, purpose: 96 },
  ];

  return (
    <Widget className="purpose-dashboard__widget reveal late">
      <Widget.Header>
          <Widget.Title>
            {fa ? "رسیدن ایده به هدف" : "An idea reaching its purpose"}
          </Widget.Title>
        <Widget.Legend>
          <Widget.LegendItem color="var(--acid)">
            {fa ? "ایده" : "Idea"}
          </Widget.LegendItem>
        </Widget.Legend>
      </Widget.Header>
      <Widget.Content className="purpose-dashboard__chart">
        <AreaChart data={data} height={360}>
          <defs>
            <linearGradient id="purpose-idea-fill" x1="0" x2="0" y1="0" y2="1">
              <stop offset="0%" stopColor="var(--acid)" stopOpacity={0.24} />
              <stop offset="100%" stopColor="var(--acid)" stopOpacity={0.02} />
            </linearGradient>
          </defs>
          <AreaChart.Area
            dataKey="idea"
            fill="url(#purpose-idea-fill)"
            name={fa ? "ایده" : "Idea"}
            stroke="var(--acid)"
            strokeWidth={2.5}
            type="monotone"
          />
          <AreaChart.Tooltip content={<AreaChart.TooltipContent />} />
        </AreaChart>
      </Widget.Content>
    </Widget>
  );
}
