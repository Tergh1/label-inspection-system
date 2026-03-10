import requests


def send_webhook(callback_url, payload):

    requests.post(callback_url, json=payload)