export type DomainLeaf = [string, string, unknown?];
export type DomainNode = DomainLeaf | '&' | '|' | '!';

export interface MetaField {
  name: string;
  labelEn: string;
  labelAr: string;
  type: string;
  operators: string[];
  relation?: string | null;
  groupable: boolean;
  sortable: boolean;
  exportable: boolean;
  requiredPermission?: string | null;
}

export interface SavedView {
  id: string;
  name: string;
  entityType: string;
  definitionJson: string;
  isShared: boolean;
  isDefault: boolean;
  sortOrder: number;
  isMine: boolean;
}

export interface OpportunityViewDefinition {
  search?: string;
  filter?: string;
  stageId?: string;
  myItemsOnly?: boolean;
  pendingOnMe?: boolean;
  isClosed?: boolean | null;
  preset?: string;
  groupBy?: string | null;
  view?: 'list' | 'kanban' | 'calendar';
  columns?: string[];
  sortBy?: string;
  sortDir?: string;
}

export function encodeDomain(nodes: DomainNode[]): string {
  return JSON.stringify(nodes);
}

export function facetLeaf(field: string, op: string, value: unknown): DomainLeaf {
  return [field, op, value];
}
