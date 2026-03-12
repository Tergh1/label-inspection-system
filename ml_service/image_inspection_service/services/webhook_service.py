import requests
from image_inspection_service.config import CLIENT_X_WEBHOOK_SECRET

def send_webhook(callback_url, payload):

    requests.post(callback_url, json=payload, headers={ "X-Webhook-Secret": f"{CLIENT_X_WEBHOOK_SECRET}" }, verify=False)