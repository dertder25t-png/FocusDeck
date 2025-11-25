import { Card, CardContent } from '../Card';

interface StatCardWidgetProps {
  label: string;
  value: string;
}

export function StatCardWidget({ label, value }: StatCardWidgetProps) {
  return (
    <Card>
      <CardContent className="pt-6">
        <div className="text-gray-400 text-sm mb-1">{label}</div>
        <div className="text-2xl font-semibold">{value}</div>
      </CardContent>
    </Card>
  );
}
