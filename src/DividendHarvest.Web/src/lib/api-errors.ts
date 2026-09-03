import axios from "axios"

export type ProblemDetails = {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  errorCode?: string
  error_code?: string
  errors?: Record<string, string[]>
}

export function getApiErrorMessage(error: unknown, fallback = "暂时无法完成请求，请稍后再试。") {
  if (axios.isAxiosError<ProblemDetails>(error)) {
    const data = error.response?.data
    const errorCode = data?.errorCode ?? data?.error_code
    if (data?.detail) {
      return data.detail
    }

    if (data?.errors) {
      const firstError = Object.values(data.errors).flat()[0]
      if (firstError) {
        return firstError
      }
    }

    if (error.code === "ECONNABORTED") {
      return "请求超时，请检查服务状态后重试。"
    }

    if (errorCode === "stock_model_parameters_not_found") {
      return "这只股票还没有模型参数，请先完成配置。"
    }
  }

  return fallback
}
