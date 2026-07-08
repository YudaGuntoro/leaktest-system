import api, { ApiRequestOptions } from "@/utils/api";
import { ApiListResponse } from "./ParameterService";

export type Issue = {
  id: number;
  number: string;
  stockInId: number;
  createdAt: string;
};

export type StockInPart = {
  id: number;
  number: string;
  name: string;
  description: string;
  isActive: boolean;
  createdAt: string;
};

export type StockIn = {
  id: number;
  code: string;
  partId: number;
  part: StockInPart;
  supplyQty: number;
  supplyDate: string;
  receiptQty: number;
  receiptDate: string;
  createdAt: string;
  issues: Issue[];
};

export type StockInQuery = {
  page?: number;
  limit?: number;
  date?: string;
  issueNumber?: string;
  partNumber?: string;
};

const StockInService = {
  getStockIns: async (
    query: StockInQuery = {},
    options?: ApiRequestOptions
  ) => {
    const response = await api.get<ApiListResponse<StockIn>>("/api/stockins", {
      ...options,
      params: query,
    });

    return response.data;
  },

  createStockIn: async (data: Partial<StockIn>, options?: ApiRequestOptions) => {
    const response = await api.post<{
      success: boolean;
      message: string;
      data: StockIn;
    }>("/api/stockins", data, options);
    return response.data;
  },

  deleteStockIn: async (id: number, options?: ApiRequestOptions) => {
    const response = await api.delete<{ success: boolean; message: string }>(
      `/api/stockins/${id}`,
      options
    );
    return response.data;
  },

  updateStockIn: async (
    id: number,
    data: Partial<StockIn>,
    options?: ApiRequestOptions
  ) => {
    const response = await api.put<{
      success: boolean;
      message: string;
      data: StockIn;
    }>(`/api/stockins/${id}`, data, options);
    return response.data;
  },
};

export default StockInService;
