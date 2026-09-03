import axios from "axios"

export const apiClient = axios.create({
  baseURL: "/api/v1",
  timeout: 20_000,
  headers: {
    Accept: "application/json",
    "Content-Type": "application/json",
  },
})

apiClient.interceptors.request.use((config) => {
  const locale = localStorage.getItem("dividend-harvest-locale") ?? "zh-CN"
  config.headers.set("Accept-Language", locale)
  return config
})
