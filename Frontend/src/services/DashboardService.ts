import api, { ApiRequestOptions } from "@/utils/api";

export type DashboardPeriodSummary = {
  totalProduction: number;
  okCount: number;
  ngCount: number;
  yieldRate: number;
};

export type DashboardSummary = {
  today: DashboardPeriodSummary;
  thisMonth: DashboardPeriodSummary;
  total: DashboardPeriodSummary;
};

export type DashboardChartItem = {
  label: string;
  value: number;
};

export type DashboardStats = {
  qualityDistribution: DashboardChartItem[];
  topPartsProduction: DashboardChartItem[];
  productionTrend: DashboardChartItem[];
};

export type DashboardLogParameter = {
  parameterId: number;
  parameterName: string;
  dataType: string;
  values: Array<string | number | boolean>;
};

export type DashboardLogDetail = {
  processName: string;
  parameters: DashboardLogParameter[];
};

export type DashboardRecentLog = {
  id: number;
  issueNo: string;
  partNumber?: string;
  partName?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
  details: DashboardLogDetail[];
};

type ApiDataResponse<T> = {
  success: boolean;
  message: string;
  data: T;
};

const DashboardService = {
  getSummary: async (options?: ApiRequestOptions) => {
    const response = await api.get<ApiDataResponse<DashboardSummary>>(
      "/api/dashboard/summary",
      options
    );

    return response.data;
  },

  getStats: async (options?: ApiRequestOptions) => {
    const response = await api.get<ApiDataResponse<DashboardStats>>(
      "/api/dashboard/stats",
      options
    );

    return response.data;
  },

  getRecentLogs: async (count = 10, options?: ApiRequestOptions) => {
    const response = await api.get<ApiDataResponse<DashboardRecentLog[]>>(
      "/api/dashboard/recent-logs",
      {
        ...options,
        params: {
          ...options?.params,
          count,
        },
      }
    );

    return response.data;
  },
};

export default DashboardService;
